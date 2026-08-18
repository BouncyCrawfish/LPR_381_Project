using LPSolver.Models;

namespace LPSolver.Algorithms;

// Converts the parsed LPModel into canonical form
// Expands sign-restricted variables ("-" is substituted, "urs" is split into two columns).
// Adds the implicit x &lt;= 1 upper-bound constraint for every "bin" variable.
// Flips any constraint with a negative RHS so every row starts non-negative.
// Appends slack/excess/artificial columns and reduces the objective row.

// = constraints are handled with the Big-M
// method, so a single Primal Simplex implementation can solve any of the three relation
// types without a separate Phase 1. Big-M is approximated with a large constant rather
// than a symbolic M; 
public static class CanonicalFormBuilder
{
    private const double BigM = 1_000_000;

    private class ColumnContribution
    {
        public required string ColumnName { get; init; }
        public required double Multiplier { get; init; }
    }

    private class ConstraintRow
    {
        public required double[] Coefficients { get; init; } // indexed by structural column
        public required RelationType Relation { get; set; }
        public required double Rhs { get; set; }
    }

    public static CanonicalForm Build(LPModel model)
    {
        bool objectiveWasNegated = model.Objective == ObjectiveType.Minimize;
        double directionSign = objectiveWasNegated ? -1.0 : 1.0;

        var (structuralColumns, contributionsPerVariable, expansions) = ExpandVariables(model);
        var columnIndex = structuralColumns
            .Select((name, idx) => (name, idx))
            .ToDictionary(p => p.name, p => p.idx);

        var objectiveStructural = new double[structuralColumns.Count];
        for (int j = 0; j < model.VariableCount; j++)
        {
            foreach (var contribution in contributionsPerVariable[j])
            {
                int idx = columnIndex[contribution.ColumnName];
                objectiveStructural[idx] += directionSign * model.ObjectiveCoefficients[j] * contribution.Multiplier;
            }
        }

        var rows = BuildConstraintRows(model, structuralColumns.Count, contributionsPerVariable, columnIndex);
        NormalizeNonNegativeRhs(rows);

        return AssembleTableau(structuralColumns, objectiveStructural, rows, objectiveWasNegated, expansions);
    }

    private static (List<string> StructuralColumns, List<ColumnContribution>[] ContributionsPerVariable, List<(int, VariableExpansion)> Expansions)
        ExpandVariables(LPModel model)
    {
        var structuralColumns = new List<string>();
        var contributionsPerVariable = new List<ColumnContribution>[model.VariableCount];
        var expansions = new List<(int, VariableExpansion)>();

        for (int j = 0; j < model.VariableCount; j++)
        {
            string baseName = $"x{j + 1}";
            var contributions = new List<ColumnContribution>();

            switch (model.SignRestrictions[j])
            {
                case SignRestrictionType.Positive:
                case SignRestrictionType.Integer:
                case SignRestrictionType.Binary:
                    structuralColumns.Add(baseName);
                    contributions.Add(new ColumnContribution { ColumnName = baseName, Multiplier = 1.0 });
                    expansions.Add((j, new VariableExpansion(baseName, baseName, null, false)));
                    break;

                case SignRestrictionType.Negative:
                    // x_j is negative. Substitute x_j = -x_j' so the simplex only ever
                    // deals with positive columns; every appearance of x_j gets * -1.
                    structuralColumns.Add(baseName);
                    contributions.Add(new ColumnContribution { ColumnName = baseName, Multiplier = -1.0 });
                    expansions.Add((j, new VariableExpansion(baseName, baseName, null, true)));
                    break;

                case SignRestrictionType.Unrestricted:
                    // x_j is free in sign. Split into x_j = x_j+ - x_j-, both positive.
                    string plus = baseName + "_plus";
                    string minus = baseName + "_minus";
                    structuralColumns.Add(plus);
                    structuralColumns.Add(minus);
                    contributions.Add(new ColumnContribution { ColumnName = plus, Multiplier = 1.0 });
                    contributions.Add(new ColumnContribution { ColumnName = minus, Multiplier = -1.0 });
                    expansions.Add((j, new VariableExpansion(baseName, plus, minus, false)));
                    break;

                default:
                    throw new InvalidOperationException($"Unhandled sign restriction for variable {baseName}");
            }

            contributionsPerVariable[j] = contributions;
        }

        return (structuralColumns, contributionsPerVariable, expansions);
    }

    private static List<ConstraintRow> BuildConstraintRows(
        LPModel model,
        int structuralColumnCount,
        List<ColumnContribution>[] contributionsPerVariable,
        Dictionary<string, int> columnIndex)
    {
        var rows = new List<ConstraintRow>();

        foreach (var constraint in model.Constraints)
        {
            var coeffs = new double[structuralColumnCount];
            for (int j = 0; j < model.VariableCount; j++)
            {
                foreach (var contribution in contributionsPerVariable[j])
                {
                    int idx = columnIndex[contribution.ColumnName];
                    coeffs[idx] += constraint.Coefficients[j] * contribution.Multiplier;
                }
            }

            rows.Add(new ConstraintRow { Coefficients = coeffs, Relation = constraint.Relation, Rhs = constraint.Rhs });
        }

        // Binary variables need x <= 1 sign restiction only has x >= 0.

        for (int j = 0; j < model.VariableCount; j++)
        {
            if (model.SignRestrictions[j] != SignRestrictionType.Binary) continue;

            var coeffs = new double[structuralColumnCount];
            coeffs[columnIndex[$"x{j + 1}"]] = 1.0;
            rows.Add(new ConstraintRow { Coefficients = coeffs, Relation = RelationType.LessThanOrEqual, Rhs = 1.0 });
        }

        return rows;
    }

    private static void NormalizeNonNegativeRhs(List<ConstraintRow> rows)
    {
        foreach (var row in rows)
        {
            if (row.Rhs >= 0) continue;

            for (int c = 0; c < row.Coefficients.Length; c++)
                row.Coefficients[c] = -row.Coefficients[c];
            row.Rhs = -row.Rhs;
            row.Relation = row.Relation switch
            {
                RelationType.LessThanOrEqual => RelationType.GreaterThanOrEqual,
                RelationType.GreaterThanOrEqual => RelationType.LessThanOrEqual,
                RelationType.Equal => RelationType.Equal,
                _ => throw new InvalidOperationException("Unhandled relation type.")
            };
        }
    }

    private static CanonicalForm AssembleTableau(
        List<string> structuralColumns,
        double[] objectiveStructural,
        List<ConstraintRow> rows,
        bool objectiveWasNegated,
        List<(int, VariableExpansion)> expansions)
    {
        int structuralCount = structuralColumns.Count;

        var extraColumnNames = new List<string>();
        var extraColumnOwnerRow = new List<int>();
        var extraColumnSign = new List<double>();
        var basisNames = new string[rows.Count];
        var artificialRows = new List<int>();

        for (int r = 0; r < rows.Count; r++)
        {
            int rowNumber = r + 1;
            switch (rows[r].Relation)
            {
                case RelationType.LessThanOrEqual:
                    string slack = $"s{rowNumber}";
                    extraColumnNames.Add(slack);
                    extraColumnOwnerRow.Add(r);
                    extraColumnSign.Add(1.0);
                    basisNames[r] = slack;
                    break;

                case RelationType.GreaterThanOrEqual:
                    string surplus = $"e{rowNumber}";
                    string artificialGe = $"a{rowNumber}";
                    extraColumnNames.Add(surplus);
                    extraColumnOwnerRow.Add(r);
                    extraColumnSign.Add(-1.0);

                    extraColumnNames.Add(artificialGe);
                    extraColumnOwnerRow.Add(r);
                    extraColumnSign.Add(1.0);

                    basisNames[r] = artificialGe;
                    artificialRows.Add(r);
                    break;

                case RelationType.Equal:
                    string artificialEq = $"a{rowNumber}";
                    extraColumnNames.Add(artificialEq);
                    extraColumnOwnerRow.Add(r);
                    extraColumnSign.Add(1.0);

                    basisNames[r] = artificialEq;
                    artificialRows.Add(r);
                    break;
            }
        }

        int totalColumns = structuralCount + extraColumnNames.Count;
        int totalRows = rows.Count + 1; // + objective row
        var matrix = new double[totalRows, totalColumns + 1]; // +1 for RHS

        for (int r = 0; r < rows.Count; r++)
        {
            for (int c = 0; c < structuralCount; c++)
                matrix[r + 1, c] = rows[r].Coefficients[c];
            matrix[r + 1, totalColumns] = rows[r].Rhs;
        }

        for (int e = 0; e < extraColumnNames.Count; e++)
        {
            int col = structuralCount + e;
            int ownerRow = extraColumnOwnerRow[e];
            matrix[ownerRow + 1, col] = extraColumnSign[e];
        }

        // Objective row: cost of structural columns = -objectiveStructural, slack/surplus = 0,
        // artificial = +BigM (their true cost is -BigM, and the row stores -cost).
        for (int c = 0; c < structuralCount; c++)
            matrix[0, c] = -objectiveStructural[c];

        for (int e = 0; e < extraColumnNames.Count; e++)
        {
            int col = structuralCount + e;
            matrix[0, col] = extraColumnNames[e].StartsWith('a') ? BigM : 0.0;
        }
        matrix[0, totalColumns] = 0.0;

        // Reduce the objective row so every basic (artificial) column reads zero -
        // the standard Big-M initial-tableau step.
        foreach (int r in artificialRows)
        {
            int artificialCol = structuralCount + extraColumnNames.IndexOf(basisNames[r]);
            double factor = matrix[0, artificialCol];
            if (Math.Abs(factor) < 1e-12) continue;

            for (int c = 0; c <= totalColumns; c++)
                matrix[0, c] -= factor * matrix[r + 1, c];
        }

        var allColumnNames = structuralColumns.Concat(extraColumnNames).ToArray();

        return new CanonicalForm(matrix, allColumnNames, basisNames, isMaximization: true, objectiveWasNegated, expansions);
    }
}

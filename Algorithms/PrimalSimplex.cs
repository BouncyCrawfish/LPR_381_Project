namespace LPSolver.Algorithms;

public enum SimplexStatus
{
    Optimal,
    Infeasible,
    Unbounded
}

//preview of the tableau after one pivot, kept so the full iteration history can be printed.
public class SimplexIterationSnapshot
{
    public double[,] Matrix { get; }
    public string[] BasisNames { get; }
    public string? EnteringVariable { get; }
    public string? LeavingVariable { get; }
    public int IterationNumber { get; }

    public SimplexIterationSnapshot(double[,] matrix, string[] basisNames, string? enteringVariable, string? leavingVariable, int iterationNumber)
    {
        Matrix = matrix;
        BasisNames = basisNames;
        EnteringVariable = enteringVariable;
        LeavingVariable = leavingVariable;
        IterationNumber = iterationNumber;
    }
}

public class SimplexResult
{
    public SimplexStatus Status { get; set; }
    public List<SimplexIterationSnapshot> Iterations { get; } = new();
    public double ObjectiveValue { get; set; }

    // Final value of every original decision variable, keyed by its name from the input file ex. (x1, x2, ...).
    public Dictionary<string, double> VariableValues { get; set; } = new();
}

/* Tableau-based Primal Simplex using the Big-M canonical form produced by
   CanonicalFormBuilder. Uses the standard "most negative reduced cost" entering rule
   and the minimum-ratio test for the leaving variable.*/

public static class PrimalSimplex
{
    private const double Epsilon = 1e-9;
    private const int MaxIterations = 1000;

    public static SimplexResult Solve(CanonicalForm canonicalForm)
    {
        var matrix = (double[,])canonicalForm.Matrix.Clone();
        var basis = (string[])canonicalForm.BasisNames.Clone();
        var result = new SimplexResult();

        int iteration = 0;
        while (true)
        {
            if (iteration > MaxIterations)
                throw new InvalidOperationException(
                    "Primal Simplex did not converge within the iteration limit - check the model for degeneracy or cycling.");

            int pivotCol = FindEnteringColumn(matrix);
            if (pivotCol == -1)
            {
                result.Status = SimplexStatus.Optimal;
                break;
            }

            int pivotRow = FindLeavingRow(matrix, pivotCol);
            if (pivotRow == -1)
            {
                result.Status = SimplexStatus.Unbounded;
                result.ObjectiveValue = double.NaN;
                result.Iterations.Add(new SimplexIterationSnapshot(
                    (double[,])matrix.Clone(), (string[])basis.Clone(), canonicalForm.ColumnNames[pivotCol], null, iteration + 1));
                return result;
            }

            string entering = canonicalForm.ColumnNames[pivotCol];
            string leaving = basis[pivotRow - 1];

            Pivot(matrix, pivotRow, pivotCol);
            basis[pivotRow - 1] = entering;
            iteration++;

            result.Iterations.Add(new SimplexIterationSnapshot(
                (double[,])matrix.Clone(), (string[])basis.Clone(), entering, leaving, iteration));
        }

        // An artificial variable left in the basis at a positive value means the original
        // model has no feasible solution - the Big-M penalty couldn't drive it out.
        int rhsCol = matrix.GetLength(1) - 1;
        for (int r = 0; r < basis.Length; r++)
        {
            if (IsArtificialColumn(basis[r]) && Math.Abs(matrix[r + 1, rhsCol]) > 1e-6)
            {
                result.Status = SimplexStatus.Infeasible;
                break;
            }
        }

        if (result.Status == SimplexStatus.Optimal)
        {
            double rawObjective = matrix[0, rhsCol];
            result.ObjectiveValue = canonicalForm.ObjectiveWasNegated ? -rawObjective : rawObjective;
            result.VariableValues = ExtractVariableValues(canonicalForm, matrix, basis);
        }

        return result;
    }

    private static bool IsArtificialColumn(string name) => name.Length > 1 && name[0] == 'a' && char.IsDigit(name[1]);

    private static int FindEnteringColumn(double[,] matrix)
    {
        int cols = matrix.GetLength(1) - 1; // exclude RHS
        int best = -1;
        double bestValue = -Epsilon;

        for (int c = 0; c < cols; c++)
        {
            if (matrix[0, c] < bestValue)
            {
                bestValue = matrix[0, c];
                best = c;
            }
        }

        return best;
    }

    private static int FindLeavingRow(double[,] matrix, int pivotCol)
    {
        int rows = matrix.GetLength(0);
        int rhsCol = matrix.GetLength(1) - 1;
        int best = -1;
        double bestRatio = double.PositiveInfinity;

        for (int r = 1; r < rows; r++)
        {
            double coeff = matrix[r, pivotCol];
            if (coeff <= Epsilon) continue;

            double ratio = matrix[r, rhsCol] / coeff;
            if (ratio < bestRatio - Epsilon)
            {
                bestRatio = ratio;
                best = r;
            }
        }

        return best;
    }

    private static void Pivot(double[,] matrix, int pivotRow, int pivotCol)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        double pivotValue = matrix[pivotRow, pivotCol];

        for (int c = 0; c < cols; c++)
            matrix[pivotRow, c] /= pivotValue;

        for (int r = 0; r < rows; r++)
        {
            if (r == pivotRow) continue;
            double factor = matrix[r, pivotCol];
            if (Math.Abs(factor) < Epsilon) continue;

            for (int c = 0; c < cols; c++)
                matrix[r, c] -= factor * matrix[pivotRow, c];
        }
    }

    private static Dictionary<string, double> ExtractVariableValues(CanonicalForm cf, double[,] matrix, string[] basis)
    {
        var raw = new Dictionary<string, double>();
        foreach (var colName in cf.ColumnNames)
            raw[colName] = 0.0;

        int rhsCol = matrix.GetLength(1) - 1;
        for (int r = 0; r < basis.Length; r++)
            raw[basis[r]] = matrix[r + 1, rhsCol];

        var final = new Dictionary<string, double>();
        foreach (var (_, expansion) in cf.VariableExpansions)
        {
            double value = expansion.IsNegatedSubstitution
                ? -raw[expansion.PositivePartColumn]
                : expansion.NegativePartColumn is null
                    ? raw[expansion.PositivePartColumn]
                    : raw[expansion.PositivePartColumn] - raw[expansion.NegativePartColumn];

            final[expansion.OriginalName] = value;
        }

        return final;
    }
}

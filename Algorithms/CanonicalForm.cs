namespace LPSolver.Algorithms;

/// <summary>
/// A simplex-ready canonical form: every structural, slack, surplus and artificial
/// variable is represented as one column, every RHS value is non-negative, and the
/// objective row has already been reduced so that basic-variable columns read zero
/// (the standard Big-M initial tableau).
/// </summary>
public class CanonicalForm
{
    /// <summary>[row, col]. Row 0 is the objective row, rows 1..m are constraints. The last column is RHS.</summary>
    public double[,] Matrix { get; }

    /// <summary>One name per structural/slack/surplus/artificial variable (excludes the RHS column).</summary>
    public string[] ColumnNames { get; }

    /// <summary>One name per constraint row (length m), i.e. the currently-basic variable for that row.</summary>
    public string[] BasisNames { get; }

    /// <summary>Canonical form is always stored as a maximisation problem internally.</summary>
    public bool IsMaximization { get; }

    /// <summary>True if the original problem was "min" and the objective was negated to maximise internally.</summary>
    public bool ObjectiveWasNegated { get; }

    /// <summary>Maps each original decision variable back from its canonical-form column(s).</summary>
    public List<(int OriginalIndex, VariableExpansion Expansion)> VariableExpansions { get; }

    public CanonicalForm(
        double[,] matrix,
        string[] columnNames,
        string[] basisNames,
        bool isMaximization,
        bool objectiveWasNegated,
        List<(int, VariableExpansion)> variableExpansions)
    {
        Matrix = matrix;
        ColumnNames = columnNames;
        BasisNames = basisNames;
        IsMaximization = isMaximization;
        ObjectiveWasNegated = objectiveWasNegated;
        VariableExpansions = variableExpansions;
    }
}

/// <summary>
/// Records how one original decision variable maps onto canonical-form columns, so the
/// final simplex solution can be translated back into the variables the user typed in.
/// </summary>
public class VariableExpansion
{
    public string OriginalName { get; }
    public string PositivePartColumn { get; }
    public string? NegativePartColumn { get; }   // set only for "urs" variables
    public bool IsNegatedSubstitution { get; }    // true for "-" (non-positive) variables, where x = -x'

    public VariableExpansion(string originalName, string positivePartColumn, string? negativePartColumn, bool isNegatedSubstitution)
    {
        OriginalName = originalName;
        PositivePartColumn = positivePartColumn;
        NegativePartColumn = negativePartColumn;
        IsNegatedSubstitution = isNegatedSubstitution;
    }
}

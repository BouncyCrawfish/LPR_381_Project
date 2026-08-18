namespace LPSolver.Models;

/*
/// A single constraint row exactly as read from the input file, before any
/// canonical-form transformation (no slack/excess/artificial variables yet).
*/

public class ConstraintModel
{
    public double[] Coefficients { get; }
    public RelationType Relation { get; }
    public double Rhs { get; }

    public ConstraintModel(double[] coefficients, RelationType relation, double rhs)
    {
        Coefficients = coefficients;
        Relation = relation;
        Rhs = rhs;
    }
}

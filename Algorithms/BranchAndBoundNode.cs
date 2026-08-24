using LPSolver.Models;

namespace LPSolver.Algorithms;

/// <summary>
/// One node in the Branch & Bound tree: the LP relaxation for this node's LPModel
/// (original constraints plus every branching constraint accumulated on the path
/// from the root), plus the simplex result once it has been solved.
/// </summary>
public class BranchAndBoundNode
{
    public LPModel Model { get; }
    public int Depth { get; }
    public string BranchDescription { get; }   // e.g. "x2 <= 3" — for readable output
    public SimplexResult? Result { get; private set; }

    public BranchAndBoundNode(LPModel model, int depth, string branchDescription)
    {
        Model = model;
        Depth = depth;
        BranchDescription = branchDescription;
    }

    /// <summary>Solves this node's LP relaxation. Call once per node.</summary>
    public SimplexResult Solve()
    {
        var canonicalForm = CanonicalFormBuilder.Build(Model);
        Result = PrimalSimplex.Solve(canonicalForm);
        return Result;
    }

    /// <summary>
    /// Finds the first integer/binary-restricted variable whose solved value is not
    /// (near enough to) an integer. Returns null if the solution is already integer-feasible.
    /// </summary>
    public (int Index, string Name, double Value)? FindFractionalVariable(double tolerance = 1e-6)
    {
        if (Result is null || Result.Status != SimplexStatus.Optimal)
            return null;

        for (int j = 0; j < Model.VariableCount; j++)
        {
            var restriction = Model.SignRestrictions[j];
            if (restriction != SignRestrictionType.Integer && restriction != SignRestrictionType.Binary)
                continue;

            string name = $"x{j + 1}";
            if (!Result.VariableValues.TryGetValue(name, out double value))
                continue;

            double nearest = Math.Round(value);
            if (Math.Abs(value - nearest) > tolerance)
                return (j, name, value);
        }

        return null; // every integer/binary variable is already integral
    }

    
    /// Builds the two child nodes for branching on variable index `varIndex` at fractional
    /// value `value`: one with x_varIndex &lt;= floor(value), one with x_varIndex &gt;= ceil(value).
    
    public (BranchAndBoundNode Floor, BranchAndBoundNode Ceiling) Branch(int varIndex, double value)
    {
        double floorRhs = Math.Floor(value);
        double ceilRhs = Math.Ceiling(value);
        string varName = $"x{varIndex + 1}";

        var floorModel = WithExtraConstraint(varIndex, RelationType.LessThanOrEqual, floorRhs);
        var ceilModel = WithExtraConstraint(varIndex, RelationType.GreaterThanOrEqual, ceilRhs);

        var floorNode = new BranchAndBoundNode(floorModel, Depth + 1, $"{varName} <= {floorRhs}");
        var ceilNode = new BranchAndBoundNode(ceilModel, Depth + 1, $"{varName} >= {ceilRhs}");

        return (floorNode, ceilNode);
    }

    private LPModel WithExtraConstraint(int varIndex, RelationType relation, double rhs)
    {
        var coefficients = new double[Model.VariableCount];
        coefficients[varIndex] = 1.0;

        var newConstraints = new List<ConstraintModel>(Model.Constraints)
        {
            new ConstraintModel(coefficients, relation, rhs)
        };

        return new LPModel(
            Model.Objective,
            Model.ObjectiveCoefficients,
            newConstraints,
            Model.SignRestrictions);
    }
}
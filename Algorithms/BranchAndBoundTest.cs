using LPSolver.Models;

namespace LPSolver.Algorithms;

/// <summary>
/// Throwaway manual test for BranchAndBoundSimplex, using the sample knapsack
/// problem from the assignment brief:
///   max +2 +3 +3 +5 +2 +4
///   +11 +8 +6 +14 +10 +10 <= 40
///   bin bin bin bin bin bin
/// Call BranchAndBoundTest.Run() from Program.cs temporarily to sanity-check
/// the B&B logic before wiring it into the real menu/output pipeline.
/// </summary>
public static class BranchAndBoundTest
{
    public static void Run()
    {
        var model = BuildSampleKnapsackModel();

        var result = BranchAndBoundSimplex.Solve(model);

        Console.WriteLine($"Nodes explored: {result.ExploredNodes.Count}");
        Console.WriteLine($"Infeasible: {result.IsInfeasible}");

        if (result.BestNode is not null && result.BestNode.Result is not null)
        {
            Console.WriteLine($"Best objective value: {Math.Round(result.BestNode.Result.ObjectiveValue, 3)}");
            Console.WriteLine("Best variable values:");
            foreach (var (name, value) in result.BestNode.Result.VariableValues)
                Console.WriteLine($"  {name} = {Math.Round(value, 3)}");
        }
        else
        {
            Console.WriteLine("No integer-feasible solution found.");
        }

        Console.WriteLine();
        Console.WriteLine("Node summary:");
        foreach (var node in result.ExploredNodes)
        {
            string status = node.Result?.Status.ToString() ?? "unsolved";
            Console.WriteLine($"  Depth {node.Depth,2} | {node.BranchDescription,-15} | {status}");
        }
    }

    private static LPModel BuildSampleKnapsackModel()
    {
        // max +2 +3 +3 +5 +2 +4
        var objectiveCoefficients = new double[] { 2, 3, 3, 5, 2, 4 };

        // +11 +8 +6 +14 +10 +10 <= 40
        var constraint = new ConstraintModel(
            coefficients: new double[] { 11, 8, 6, 14, 10, 10 },
            relation: RelationType.LessThanOrEqual,
            rhs: 40);

        // bin bin bin bin bin bin
        var signRestrictions = new[]
        {
            SignRestrictionType.Binary,
            SignRestrictionType.Binary,
            SignRestrictionType.Binary,
            SignRestrictionType.Binary,
            SignRestrictionType.Binary,
            SignRestrictionType.Binary
        };

        return new LPModel(
            ObjectiveType.Maximize,
            objectiveCoefficients,
            new List<ConstraintModel> { constraint },
            signRestrictions);
    }
}
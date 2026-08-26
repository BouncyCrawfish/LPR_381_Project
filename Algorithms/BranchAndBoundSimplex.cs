using LPSolver.Models;

namespace LPSolver.Algorithms;

/// <summary>
/// The overall result of a Branch & Bound run: the best integer-feasible node found
/// (or null if the model is infeasible), plus every node that was explored, in the
/// order they were solved - so their tableau iterations can all be printed.
/// </summary>
public class BranchAndBoundResult
{
    public BranchAndBoundNode? BestNode { get; set; }
    public List<BranchAndBoundNode> ExploredNodes { get; } = new();
    public bool IsInfeasible { get; set; }
}

/// <summary>
/// Branch & Bound using the Primal Simplex to solve each node's LP relaxation.
/// Explores nodes depth-first (a stack gives natural backtracking), fathoming
/// infeasible nodes, bound-dominated nodes, and accepting integer-feasible nodes
/// as candidates for the best solution.
/// </summary>
public static class BranchAndBoundSimplex
{
    public static BranchAndBoundResult Solve(LPModel rootModel)
    {
        var result = new BranchAndBoundResult();
        var stack = new Stack<BranchAndBoundNode>();
        stack.Push(new BranchAndBoundNode(rootModel, depth: 0, branchDescription: "root"));

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            var simplexResult = node.Solve();
            result.ExploredNodes.Add(node);

            // Fathom: infeasible sub-problem - prune this branch entirely.
            if (simplexResult.Status == SimplexStatus.Infeasible)
                continue;

            // Unbounded should only really happen at the root; treat it as a dead end
            // for this node rather than crashing the whole run.
            if (simplexResult.Status == SimplexStatus.Unbounded)
                continue;

            // Fathom: this node's relaxed objective can't beat the current best
            // integer solution, so no point exploring further down this branch.
            if (IsWorseThanIncumbent(rootModel.Objective, simplexResult.ObjectiveValue, result.BestNode))
                continue;

            var fractional = node.FindFractionalVariable();

            if (fractional is null)
            {
                // Integer-feasible - candidate for best solution.
                if (IsBetterCandidate(rootModel.Objective, simplexResult.ObjectiveValue, result.BestNode))
                    result.BestNode = node;

                continue;
            }

            // Not integer-feasible - branch on the fractional variable and keep exploring.
            var (floorNode, ceilNode) = node.Branch(fractional.Value.Index, fractional.Value.Value);
            stack.Push(floorNode);
            stack.Push(ceilNode);
        }

        result.IsInfeasible = result.BestNode is null;
        return result;
    }

    private static bool IsBetterCandidate(ObjectiveType objective, double candidateValue, BranchAndBoundNode? incumbent)
    {
        if (incumbent?.Result is null)
            return true;

        double incumbentValue = incumbent.Result.ObjectiveValue;
        return objective == ObjectiveType.Maximize
            ? candidateValue > incumbentValue
            : candidateValue < incumbentValue;
    }

    private static bool IsWorseThanIncumbent(ObjectiveType objective, double nodeValue, BranchAndBoundNode? incumbent)
    {
        if (incumbent?.Result is null)
            return false; // no incumbent yet - nothing to bound against

        double incumbentValue = incumbent.Result.ObjectiveValue;
        const double tolerance = 1e-6;

        return objective == ObjectiveType.Maximize
            ? nodeValue <= incumbentValue + tolerance
            : nodeValue >= incumbentValue - tolerance;
    }
}
using LPSolver.Models;

namespace LPSolver.Algorithms;

/// <summary>
/// One item in a 0/1 knapsack problem, with its original index preserved so the
/// final solution can be reported back against the original decision variable names.
/// </summary>
public class KnapsackItem
{
    public int OriginalIndex { get; }
    public double Value { get; }
    public double Weight { get; }
    public double Ratio => Weight > 0 ? Value / Weight : double.PositiveInfinity;

    public KnapsackItem(int originalIndex, double value, double weight)
    {
        OriginalIndex = originalIndex;
        Value = value;
        Weight = weight;
    }
}

/// <summary>
/// One node in the Knapsack Branch &amp; Bound tree: a partial assignment (some items
/// forced in, some forced out, the rest still undecided), plus its bound.
/// </summary>
public class KnapsackNode
{
    public int Depth { get; }                 // index into the ratio-sorted item list of the next undecided item
    public bool?[] Decisions { get; }          // true = included, false = excluded, null = undecided; indexed by sorted position
    public double CurrentValue { get; }        // value of everything already decided "included"
    public double CurrentWeight { get; }       // weight of everything already decided "included"
    public double Bound { get; }               // relaxed (fractional) upper bound on this node's best possible value
    public string BranchDescription { get; }

    public KnapsackNode(int depth, bool?[] decisions, double currentValue, double currentWeight, double bound, string branchDescription)
    {
        Depth = depth;
        Decisions = decisions;
        CurrentValue = currentValue;
        CurrentWeight = currentWeight;
        Bound = bound;
        BranchDescription = branchDescription;
    }
}

public class KnapsackResult
{
    public KnapsackNode? BestNode { get; set; }
    public Dictionary<string, double> BestVariableValues { get; set; } = new();
    public double BestObjectiveValue { get; set; }
    public List<KnapsackNode> ExploredNodes { get; } = new();
    public bool IsInfeasible { get; set; }
}

/// <summary>
/// Branch &amp; Bound Knapsack: a separate algorithm from Branch &amp; Bound Simplex.
/// Items are sorted once by value/weight ratio. Each node's bound is computed by
/// greedily filling the remaining capacity in ratio order and taking the fractional
/// part of the item that doesn't fully fit (the classic fractional-knapsack bound) -
/// no simplex re-solve per node. Explored depth-first (stack) for natural backtracking,
/// with fathoming on both infeasible (over capacity) and bound-dominated nodes.
/// </summary>
public static class BranchAndBoundKnapsack
{
    public static KnapsackResult Solve(LPModel model)
    {
        ValidateIsKnapsackShaped(model);

        var items = BuildRatioSortedItems(model);
        double capacity = model.Constraints[0].Rhs;

        var result = new KnapsackResult();
        var stack = new Stack<KnapsackNode>();

        var rootDecisions = new bool?[items.Count];
        double rootBound = ComputeBound(items, rootDecisions, depth: 0, currentValue: 0, currentWeight: 0, capacity);
        stack.Push(new KnapsackNode(0, rootDecisions, 0, 0, rootBound, "root"));

        double bestValue = double.NegativeInfinity;

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            result.ExploredNodes.Add(node);

            // Fathom: over capacity - infeasible branch.
            if (node.CurrentWeight > capacity)
                continue;

            // Fathom: even the optimistic (fractional) bound can't beat the best
            // integer solution found so far - no point exploring further.
            if (node.Bound <= bestValue + 1e-9)
                continue;

            if (node.Depth == items.Count)
            {
                // All items decided - integer-feasible candidate.
                if (node.CurrentValue > bestValue)
                {
                    bestValue = node.CurrentValue;
                    result.BestNode = node;
                }
                continue;
            }

            // Branch on the next undecided item: exclude, then include.
            var excludeDecisions = (bool?[])node.Decisions.Clone();
            excludeDecisions[node.Depth] = false;
            double excludeBound = ComputeBound(items, excludeDecisions, node.Depth + 1, node.CurrentValue, node.CurrentWeight, capacity);
            var excludeNode = new KnapsackNode(
                node.Depth + 1, excludeDecisions, node.CurrentValue, node.CurrentWeight, excludeBound,
                $"exclude x{items[node.Depth].OriginalIndex + 1}");

            var includeDecisions = (bool?[])node.Decisions.Clone();
            includeDecisions[node.Depth] = true;
            double includeValue = node.CurrentValue + items[node.Depth].Value;
            double includeWeight = node.CurrentWeight + items[node.Depth].Weight;
            double includeBound = ComputeBound(items, includeDecisions, node.Depth + 1, includeValue, includeWeight, capacity);
            var includeNode = new KnapsackNode(
                node.Depth + 1, includeDecisions, includeValue, includeWeight, includeBound,
                $"include x{items[node.Depth].OriginalIndex + 1}");

            stack.Push(excludeNode);
            stack.Push(includeNode);
        }

        result.IsInfeasible = result.BestNode is null;

        if (result.BestNode is not null)
        {
            result.BestObjectiveValue = result.BestNode.CurrentValue;
            result.BestVariableValues = ExtractVariableValues(items, result.BestNode.Decisions, model.VariableCount);
        }

        return result;
    }

    /// <summary>
    /// Computes the relaxed (fractional) upper bound for a node: take everything
    /// already decided, then greedily add undecided items in ratio order until
    /// capacity is hit, taking a fractional slice of the item that overflows.
    /// </summary>
    private static double ComputeBound(List<KnapsackItem> items, bool?[] decisions, int depth, double currentValue, double currentWeight, double capacity)
    {
        double bound = currentValue;
        double remainingCapacity = capacity - currentWeight;

        for (int i = depth; i < items.Count; i++)
        {
            if (remainingCapacity <= 0) break;

            if (items[i].Weight <= remainingCapacity)
            {
                bound += items[i].Value;
                remainingCapacity -= items[i].Weight;
            }
            else
            {
                // Fractional slice of this item - this is what makes the bound
                // an LP relaxation rather than another integer solution.
                bound += items[i].Value * (remainingCapacity / items[i].Weight);
                remainingCapacity = 0;
            }
        }

        return bound;
    }

    private static List<KnapsackItem> BuildRatioSortedItems(LPModel model)
    {
        var constraint = model.Constraints[0];
        var items = new List<KnapsackItem>();

        for (int j = 0; j < model.VariableCount; j++)
            items.Add(new KnapsackItem(j, model.ObjectiveCoefficients[j], constraint.Coefficients[j]));

        return items.OrderByDescending(i => i.Ratio).ToList();
    }

    private static Dictionary<string, double> ExtractVariableValues(List<KnapsackItem> sortedItems, bool?[] decisions, int variableCount)
    {
        var values = new Dictionary<string, double>();
        for (int j = 0; j < variableCount; j++)
            values[$"x{j + 1}"] = 0.0;

        for (int i = 0; i < sortedItems.Count; i++)
        {
            if (decisions[i] == true)
                values[$"x{sortedItems[i].OriginalIndex + 1}"] = 1.0;
        }

        return values;
    }

    /// <summary>
    /// Branch and Bound Knapsack only applies to a single-constraint 0/1 model:
    /// exactly one constraint (the capacity), and every variable declared "bin".
    /// </summary>
    private static void ValidateIsKnapsackShaped(LPModel model)
    {
        if (model.Constraints.Count != 1)
            throw new InvalidOperationException(
                "Branch and Bound Knapsack requires exactly one constraint (the knapsack capacity).");

        if (model.SignRestrictions.Any(r => r != SignRestrictionType.Binary))
            throw new InvalidOperationException(
                "Branch and Bound Knapsack requires every decision variable to be declared 'bin'.");

        if (model.Constraints[0].Relation != RelationType.LessThanOrEqual)
            throw new InvalidOperationException(
                "Branch and Bound Knapsack requires the capacity constraint to use '<='.");
    }
}
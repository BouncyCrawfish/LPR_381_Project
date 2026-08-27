using LPSolver.Algorithms;
using LPSolver.Models;

namespace LPSolver.IO;

// Writes Branch & Bound Simplex and Branch & Bound Knapsack results to the output
// text file - every explored sub-problem (with its full tableau iteration history
// for the Simplex variant), fathoming outcome, and the best candidate found, with
// all decimal values rounded to three places.
public static class BranchAndBoundResultWriter
{
    public static void WriteSimplex(string outputPath, LPModel model, BranchAndBoundResult result)
    {
        using var writer = new StreamWriter(outputPath, false);

        writer.WriteLine("=== BRANCH & BOUND SIMPLEX ===");
        writer.WriteLine($"Objective: {(model.Objective == ObjectiveType.Maximize ? "max" : "min")}");
        writer.WriteLine($"Decision variables: {model.VariableCount}");
        writer.WriteLine($"Nodes explored: {result.ExploredNodes.Count}");
        writer.WriteLine();

        int nodeNumber = 0;
        foreach (var node in result.ExploredNodes)
        {
            nodeNumber++;
            writer.WriteLine($"================ NODE {nodeNumber} ({node.BranchDescription}) ================");
            writer.WriteLine($"Depth: {node.Depth}");

            if (node.Result is null)
            {
                writer.WriteLine("Not solved.");
                writer.WriteLine();
                continue;
            }

            writer.WriteLine($"Status: {node.Result.Status}");

            var canonicalForm = CanonicalFormBuilder.Build(node.Model);

            writer.WriteLine("--- Canonical Form (initial tableau) ---");
            WriteTableau(writer, canonicalForm.ColumnNames, canonicalForm.BasisNames, canonicalForm.Matrix);
            writer.WriteLine();

            writer.WriteLine("--- Simplex Iterations ---");
            foreach (var snapshot in node.Result.Iterations)
            {
                writer.WriteLine(
                    $"Iteration {snapshot.IterationNumber}: entering = {snapshot.EnteringVariable ?? "-"}, leaving = {snapshot.LeavingVariable ?? "-"}");
                WriteTableau(writer, canonicalForm.ColumnNames, snapshot.BasisNames, snapshot.Matrix);
                writer.WriteLine();
            }

            if (node.Result.Status == SimplexStatus.Optimal)
            {
                writer.WriteLine($"Relaxed objective value: {Math.Round(node.Result.ObjectiveValue, 3)}");
                foreach (var kv in node.Result.VariableValues)
                    writer.WriteLine($"{kv.Key} = {Math.Round(kv.Value, 3)}");
            }

            writer.WriteLine();
        }

        writer.WriteLine("=============== BEST CANDIDATE ===============");

        if (result.IsInfeasible || result.BestNode?.Result is null)
        {
            writer.WriteLine("No integer-feasible solution found.");
            return;
        }

        writer.WriteLine($"Found at: {result.BestNode.BranchDescription} (depth {result.BestNode.Depth})");
        writer.WriteLine($"Optimal objective value: {Math.Round(result.BestNode.Result.ObjectiveValue, 3)}");
        writer.WriteLine();
        writer.WriteLine("Decision Variables");
        foreach (var kv in result.BestNode.Result.VariableValues.OrderBy(v => v.Key))
            writer.WriteLine($"{kv.Key} = {Math.Round(kv.Value, 3)}");
    }

    public static void WriteKnapsack(string outputPath, LPModel model, KnapsackResult result)
    {
        using var writer = new StreamWriter(outputPath, false);

        writer.WriteLine("=== BRANCH & BOUND KNAPSACK ===");
        writer.WriteLine($"Decision variables: {model.VariableCount}");
        writer.WriteLine($"Nodes explored: {result.ExploredNodes.Count}");
        writer.WriteLine();

        int nodeNumber = 0;
        foreach (var node in result.ExploredNodes)
        {
            nodeNumber++;
            writer.WriteLine($"Node {nodeNumber} ({node.BranchDescription}): depth = {node.Depth}, " +
                $"value = {Math.Round(node.CurrentValue, 3)}, weight = {Math.Round(node.CurrentWeight, 3)}, " +
                $"bound = {Math.Round(node.Bound, 3)}");
        }

        writer.WriteLine();
        writer.WriteLine("=============== BEST CANDIDATE ===============");

        if (result.IsInfeasible || result.BestNode is null)
        {
            writer.WriteLine("No integer-feasible solution found.");
            return;
        }

        writer.WriteLine($"Optimal objective value: {Math.Round(result.BestObjectiveValue, 3)}");
        writer.WriteLine();
        writer.WriteLine("Decision Variables");
        foreach (var kv in result.BestVariableValues.OrderBy(v => v.Key))
            writer.WriteLine($"{kv.Key} = {Math.Round(kv.Value, 3)}");
    }

    private static void WriteTableau(StreamWriter writer, string[] columnNames, string[] basisNames, double[,] matrix)
    {
        int cols = matrix.GetLength(1); // includes RHS

        writer.Write("Basis\t");
        foreach (var name in columnNames)
            writer.Write($"{name}\t");
        writer.WriteLine("RHS");

        writer.Write("Z\t");
        for (int c = 0; c < cols; c++)
            writer.Write($"{Math.Round(matrix[0, c], 3)}\t");
        writer.WriteLine();

        for (int r = 1; r < matrix.GetLength(0); r++)
        {
            writer.Write($"{basisNames[r - 1]}\t");
            for (int c = 0; c < cols; c++)
                writer.Write($"{Math.Round(matrix[r, c], 3)}\t");
            writer.WriteLine();
        }
    }
}

using LPSolver.Algorithms;
using LPSolver.Models;

namespace LPSolver.IO;


// Writes the canonical form, every simplex tableau iteration and the final result to the output text file with all decimal values rounded to three places


public static class ResultWriter
{
    public static void Write(string outputPath, LPModel model, CanonicalForm canonicalForm, SimplexResult result)
    {
        using var writer = new StreamWriter(outputPath, false);

        writer.WriteLine("=== LPR381 Solver Output ===");
        writer.WriteLine($"Objective: {(model.Objective == ObjectiveType.Maximize ? "max" : "min")}");
        writer.WriteLine($"Decision variables: {model.VariableCount}");
        writer.WriteLine($"Constraints: {model.Constraints.Count}");
        writer.WriteLine();

        writer.WriteLine("--- Canonical Form (initial tableau) ---");
        WriteTableau(writer, canonicalForm.ColumnNames, canonicalForm.BasisNames, canonicalForm.Matrix);
        writer.WriteLine();

        writer.WriteLine("--- Primal Simplex Iterations ---");
        foreach (var snapshot in result.Iterations)
        {
            writer.WriteLine($"Iteration {snapshot.IterationNumber}: entering = {snapshot.EnteringVariable ?? "-"}, leaving = {snapshot.LeavingVariable ?? "-"}");
            WriteTableau(writer, canonicalForm.ColumnNames, snapshot.BasisNames, snapshot.Matrix);
            writer.WriteLine();
        }

        writer.WriteLine("--- Result ---");
        writer.WriteLine($"Status: {result.Status}");

        switch (result.Status)
        {
            case SimplexStatus.Optimal:
                writer.WriteLine($"Optimal objective value: {Math.Round(result.ObjectiveValue, 3)}");
                foreach (var kv in result.VariableValues)
                    writer.WriteLine($"{kv.Key} = {Math.Round(kv.Value, 3)}");
                break;

            case SimplexStatus.Infeasible:
                writer.WriteLine("The model is infeasible: an artificial variable remained in the basis at a positive value.");
                break;

            case SimplexStatus.Unbounded:
                writer.WriteLine("The model is unbounded: the entering variable's ratio test found no limiting row.");
                break;
        }
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

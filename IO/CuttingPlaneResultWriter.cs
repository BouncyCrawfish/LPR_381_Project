using LPSolver.Algorithms;
using System;
using System.IO;

namespace LPSolver.IO;

public static class CuttingPlaneResultWriter
{
    public static void Write(
        string outputPath,
        CuttingPlaneResult result)
    {
        using var writer =
            new StreamWriter(outputPath, false);

        writer.WriteLine("=== CUTTING PLANE ALGORITHM ===");
        writer.WriteLine();

        writer.WriteLine($"Integer Feasible: {result.IsIntegerFeasible}");
        writer.WriteLine();

        foreach (var cut in result.Iterations)
        {
            writer.WriteLine(
                $"================ CUT {cut.IterationNumber} ================");

            writer.WriteLine(
                $"Selected Variable : {cut.SelectedVariable}");

            writer.WriteLine(
                $"Selected Value    : {Math.Round(cut.SelectedValue, 6)}");

            writer.WriteLine(
                $"Generated Cut     : {cut.GeneratedCut}");

            writer.WriteLine();

            writer.WriteLine("--- Simplex Iterations ---");

            foreach (var snapshot in cut.SimplexResult.Iterations)
            {
                writer.WriteLine(
                    $"Iteration {snapshot.IterationNumber}");

                writer.WriteLine(
                    $"Entering Variable: {snapshot.EnteringVariable ?? "-"}");

                writer.WriteLine(
                    $"Leaving Variable : {snapshot.LeavingVariable ?? "-"}");

                writer.WriteLine();
            }

            writer.WriteLine();
        }

        writer.WriteLine("=============== FINAL RESULT ===============");

        if (result.FinalResult == null)
        {
            writer.WriteLine("No final solution found.");
            return;
        }

        writer.WriteLine(
            $"Status: {result.FinalResult.Status}");

        if (result.FinalResult.Status ==
            SimplexStatus.Optimal)
        {
            writer.WriteLine(
                $"Objective Value: {Math.Round(result.FinalResult.ObjectiveValue, 6)}");

            writer.WriteLine();

            writer.WriteLine("Decision Variables");

            foreach (var variable
                     in result.FinalResult.VariableValues
                         .OrderBy(v => v.Key))
            {
                writer.WriteLine(
                    $"{variable.Key} = {Math.Round(variable.Value, 6)}");
            }
        }
    }
}
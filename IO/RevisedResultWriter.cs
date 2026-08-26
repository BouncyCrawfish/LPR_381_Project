using LPSolver.Algorithms;
using LPSolver.Models;
using System;
using System.IO;

namespace LPSolver.IO;

public static class RevisedResultWriter
{
    public static void Write(
        string outputPath,
        LPModel model,
        CanonicalForm canonicalForm,
        RevisedSimplexResult result)
    {
        using var writer = new StreamWriter(outputPath, false);

        writer.WriteLine("=== REVISED PRIMAL SIMPLEX ===");
        writer.WriteLine();

        foreach (var iteration in result.Iterations)
        {
            writer.WriteLine($"T-{iteration.IterationNumber}");
            writer.WriteLine();

            writer.WriteLine("Basis Variables:");
            writer.WriteLine(string.Join(", ",
                iteration.BasisVariables));

            writer.WriteLine();

            writer.WriteLine("Non-Basis Variables:");
            writer.WriteLine(string.Join(", ",
                iteration.NonBasisVariables));

            writer.WriteLine();

            writer.WriteLine("Reduced Costs");

            foreach (var rc in iteration.ReducedCosts)
            {
                writer.WriteLine(
                    $"{rc.Key} = {Math.Round(rc.Value, 3)}");
            }

            writer.WriteLine();

            writer.WriteLine(
                $"Entering Variable: {iteration.EnteringVariable}");

            writer.WriteLine(
                $"Leaving Variable: {iteration.LeavingVariable}");

            writer.WriteLine(new string('-', 50));
        }

        writer.WriteLine();
        writer.WriteLine("Final Result");
        writer.WriteLine($"Status: {result.Status}");
        writer.WriteLine(
            $"Objective Value: {Math.Round(result.ObjectiveValue, 3)}");

        foreach (var variable in result.VariableValues)
        {
            writer.WriteLine(
                $"{variable.Key} = {Math.Round(variable.Value, 3)}");
        }
    }
}
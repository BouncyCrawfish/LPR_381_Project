using LPSolver.Algorithms;
using LPSolver.IO;

namespace LPSolver;

/*
 Usage:
 solve.exe [inputFilePath] [outputFilePath]

 If no arguments are given, you'll be prompted for both paths.
*/
public static class Program
{
    public static void Main(string[] args)
    {
        string inputPath =
            args.Length > 0
                ? args[0]
                : PromptForPath("Enter path to input file: ");

        string outputPath =
            args.Length > 1
                ? args[1]
                : PromptForPath("Enter path for output file: ");

        try
        {
            var model = ModelParser.Parse(inputPath);
            var canonicalForm = CanonicalFormBuilder.Build(model);

            Console.WriteLine();
            Console.WriteLine("Choose algorithm:");
            Console.WriteLine("1 - Primal Simplex");
            Console.WriteLine("2 - Revised Primal Simplex");
            Console.WriteLine("3 - Cutting Plane");
            Console.WriteLine("4 - Branch & Bound Simplex");
            Console.WriteLine("5 - Branch & Bound Knapsack");
            Console.Write("Selection: ");

            string choice = Console.ReadLine() ?? "1";

            if (choice == "2")
            {
                var revisedResult =
                    RevisedPrimalSimplex.Solve(canonicalForm);

                RevisedResultWriter.Write(
                    outputPath,
                    model,
                    canonicalForm,
                    revisedResult);

                Console.WriteLine(
                    $"Done. Status: {revisedResult.Status}");

                if (revisedResult.Status ==
                    SimplexStatus.Optimal)
                {
                    Console.WriteLine(
                        $"Optimal value: {Math.Round(revisedResult.ObjectiveValue, 3)}");
                }
            }
            else if (choice == "3")
            {
                var result =
                    CuttingPlane.Solve(model);

                CuttingPlaneResultWriter.Write(
                    outputPath,
                    result);

                Console.WriteLine(
                    $"Done. Integer Feasible: {result.IsIntegerFeasible}");

                if (result.FinalResult != null &&
                    result.FinalResult.Status == SimplexStatus.Optimal)
                {
                    Console.WriteLine(
                        $"Optimal value: {Math.Round(result.FinalResult.ObjectiveValue, 3)}");
                }
            }
            else if (choice == "4")
            {
                var bbResult = BranchAndBoundSimplex.Solve(model);

                BranchAndBoundResultWriter.WriteSimplex(outputPath, model, bbResult);

                Console.WriteLine($"Done. Nodes Explored: {bbResult.ExploredNodes.Count}");
                if (bbResult.BestNode?.Result != null)
                {
                    Console.WriteLine(
                        $"Optimal Objective Value: {Math.Round(bbResult.BestNode.Result.ObjectiveValue, 3)}");
                }
                else
                {
                    Console.WriteLine("No integer-feasible solution found.");
                }
            }

            else if (choice == "5")
            {
                var knapsackResult = BranchAndBoundKnapsack.Solve(model);

                BranchAndBoundResultWriter.WriteKnapsack(outputPath, model, knapsackResult);

                Console.WriteLine($"Done. Nodes Explored: {knapsackResult.ExploredNodes.Count}");
                if (knapsackResult.BestNode != null)
                {
                    Console.WriteLine(
                        $"Optimal Objective Value: {Math.Round(knapsackResult.BestObjectiveValue, 3)}");
                }
                else
                {
                    Console.WriteLine("No integer-feasible solution found.");
                }
            }
            else
            {
                var primalResult =
                    PrimalSimplex.Solve(canonicalForm);

                ResultWriter.Write(
                    outputPath,
                    model,
                    canonicalForm,
                    primalResult);

                Console.WriteLine(
                    $"Done. Status: {primalResult.Status}");

                if (primalResult.Status ==
                    SimplexStatus.Optimal)
                {
                    Console.WriteLine(
                        $"Optimal value: {Math.Round(primalResult.ObjectiveValue, 3)}");
                }
            }

            Console.WriteLine(
                $"Full results written to: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private static string PromptForPath(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine() ?? string.Empty;
    }
}

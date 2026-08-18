using LPSolver.Algorithms;
using LPSolver.IO;

namespace LPSolver;

/*
 
 how to use solve.exe [inputFilePath] [outputFilePath]
 If no arguments are given, you'll be prompted for both paths.
*/
public static class Program
{
    public static void Main(string[] args)
    {
        string inputPath = args.Length > 0 ? args[0] : PromptForPath("Enter path to input file: ");
        string outputPath = args.Length > 1 ? args[1] : PromptForPath("Enter path for output file: ");

        try
        {
            var model = ModelParser.Parse(inputPath);
            var canonicalForm = CanonicalFormBuilder.Build(model);
            var result = PrimalSimplex.Solve(canonicalForm);
            ResultWriter.Write(outputPath, model, canonicalForm, result);

            Console.WriteLine($"Done. Status: {result.Status}");
            if (result.Status == SimplexStatus.Optimal)
                Console.WriteLine($"Optimal value: {Math.Round(result.ObjectiveValue, 3)}");
            Console.WriteLine($"Full results written to: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static string PromptForPath(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine() ?? string.Empty;
    }
}

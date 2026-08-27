using System;
using LPR381_Project.Models;

namespace LPR381_Project.Services
{
    public static class FileParserService
    {
        public static LinearProgram Load(string path)
        {
          
            Console.WriteLine($"Reading from {path}...");
            return new LinearProgram();
        }

        public static void Export(OptimizationResult result, string path)
        {
            
            Console.WriteLine($"Exporting results to {path}...");
        }
    }
}

using System.Collections.Generic;

namespace LPR381_Project.Models
{
    public class OptimizationResult
    {
        public double[] OptimalVariables { get; set; }
        public double OptimalValue { get; set; }
        public List<string> TableauIterations { get; set; } = new List<string>();

        // Needed for Sensitivity Analysis
        public double[,] FinalTableau { get; set; }
        public int[] BasicVariables { get; set; }
    }
}

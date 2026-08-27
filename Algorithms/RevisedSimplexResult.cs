using System.Collections.Generic;

namespace LPSolver.Algorithms;

public class RevisedSimplexResult
{
    public SimplexStatus Status { get; set; }

    public List<RevisedSimplexIterationSnapshot> Iterations { get; }
        = new();

    public double ObjectiveValue { get; set; }

    public Dictionary<string, double> VariableValues { get; set; }
        = new();
}
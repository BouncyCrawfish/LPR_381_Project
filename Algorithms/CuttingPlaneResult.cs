using System.Collections.Generic;

namespace LPSolver.Algorithms;

public class CuttingPlaneResult
{
    public bool IsIntegerFeasible { get; set; }

    public SimplexResult? FinalResult { get; set; }

    public List<CuttingPlaneIteration> Iterations { get; }
        = new();
}
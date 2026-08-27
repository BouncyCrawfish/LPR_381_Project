using LPR381_Project.Models;

namespace LPR381_Project.Interfaces
{
    public interface ISolver
    {
        OptimizationResult Solve(LinearProgram model);
    }
}

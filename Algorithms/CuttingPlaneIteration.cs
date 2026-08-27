namespace LPSolver.Algorithms;

public class CuttingPlaneIteration
{
    public int IterationNumber { get; }

    public string SelectedVariable { get; }

    public double SelectedValue { get; }

    public string GeneratedCut { get; }

    public SimplexResult SimplexResult { get; }

    public CuttingPlaneIteration(
        int iterationNumber,
        string selectedVariable,
        double selectedValue,
        string generatedCut,
        SimplexResult simplexResult)
    {
        IterationNumber = iterationNumber;
        SelectedVariable = selectedVariable;
        SelectedValue = selectedValue;
        GeneratedCut = generatedCut;
        SimplexResult = simplexResult;
    }
}
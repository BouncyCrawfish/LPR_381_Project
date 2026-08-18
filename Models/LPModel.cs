namespace LPSolver.Models;

/*
 The full LP/IP model as parsed from the input text file, before conversion
 into canonical form.
*/
public class LPModel
{
    public ObjectiveType Objective { get; }
    public double[] ObjectiveCoefficients { get; }
    public List<ConstraintModel> Constraints { get; }
    public SignRestrictionType[] SignRestrictions { get; }

    public int VariableCount => ObjectiveCoefficients.Length;

    public LPModel(
        ObjectiveType objective,
        double[] objectiveCoefficients,
        List<ConstraintModel> constraints,
        SignRestrictionType[] signRestrictions)
    {
        if (signRestrictions.Length != objectiveCoefficients.Length)
            throw new ArgumentException("Sign restriction count must match the number of decision variables.");

        foreach (var constraint in constraints)
        {
            if (constraint.Coefficients.Length != objectiveCoefficients.Length)
                throw new ArgumentException("Every constraint must supply one coefficient per decision variable.");
        }

        Objective = objective;
        ObjectiveCoefficients = objectiveCoefficients;
        Constraints = constraints;
        SignRestrictions = signRestrictions;
    }
}

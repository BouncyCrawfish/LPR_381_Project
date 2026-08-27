using LPSolver.Models;
using System;

namespace LPSolver.Algorithms;

public static class CuttingPlane
{
    
    private static bool IsIntegerSolution(
    LPModel model,
    SimplexResult result,
    double tolerance = 1e-6)
    {
        for (int i = 0; i < model.VariableCount; i++)
        {
            var restriction =
                model.SignRestrictions[i];

            if (restriction != SignRestrictionType.Integer &&
                restriction != SignRestrictionType.Binary)
                continue;

            string variable = $"x{i + 1}";

            if (!result.VariableValues.TryGetValue(
                    variable,
                    out double value))
                continue;

            if (Math.Abs(
                    value - Math.Round(value))
                > tolerance)
                return false;
        }

        return true;
    }

    private static (int Index, string Name, double Value)?
FindBestFractionalVariable(
    LPModel model,
    SimplexResult result)
    {
        double bestDistance =
            double.PositiveInfinity;

        (int, string, double)? best = null;

        for (int i = 0; i < model.VariableCount; i++)
        {
            var restriction =
                model.SignRestrictions[i];

            if (restriction != SignRestrictionType.Integer &&
                restriction != SignRestrictionType.Binary)
                continue;

            string name =
                $"x{i + 1}";

            if (!result.VariableValues.TryGetValue(
                    name,
                    out double value))
                continue;

            double fractional =
                value - Math.Floor(value);

            if (fractional < 1e-6 ||
                1.0 - fractional < 1e-6)
                continue;

            double distance =
                Math.Abs(
                    fractional - 0.5);

            if (distance < bestDistance)
            {
                bestDistance = distance;

                best = (
                    i,
                    name,
                    value);
            }
        }

        return best;
    }

    private static ConstraintModel BuildCutConstraint(
    LPModel model,
    int variableIndex,
    double value)
    {
        var coefficients =
            new double[model.VariableCount];

        coefficients[variableIndex] = 1.0;

        double rhs =
            Math.Floor(value);

        return new ConstraintModel(
            coefficients,
            RelationType.LessThanOrEqual,
            rhs);
    }

    private static LPModel AddCut(
    LPModel model,
    ConstraintModel cut)
    {
        return new LPModel(
            model.Objective,
            model.ObjectiveCoefficients,
            model.Constraints
                .Append(cut)
                .ToList(),
            model.SignRestrictions);
    }

    public static CuttingPlaneResult Solve(
    LPModel model)
    {
        var result =
            new CuttingPlaneResult();

        LPModel currentModel =
            model;

        int iteration = 0;

        while (true)
        {
            var canonicalForm =
                CanonicalFormBuilder.Build(
                    currentModel);

            var simplexResult =
                PrimalSimplex.Solve(
                    canonicalForm);

            if (simplexResult.Status !=
                SimplexStatus.Optimal)
            {
                result.FinalResult =
                    simplexResult;

                result.IsIntegerFeasible =
                    false;

                return result;
            }

            if (IsIntegerSolution(
                    currentModel,
                    simplexResult))
            {
                result.FinalResult =
                    simplexResult;

                result.IsIntegerFeasible =
                    true;

                return result;
            }

            var fractional =
                FindBestFractionalVariable(
                    currentModel,
                    simplexResult);

            if (fractional is null)
            {
                result.FinalResult =
                    simplexResult;

                result.IsIntegerFeasible =
                    true;

                return result;
            }

            var cut =
                BuildCutConstraint(
                    currentModel,
                    fractional.Value.Index,
                    fractional.Value.Value);

            iteration++;

            result.Iterations.Add(
                new CuttingPlaneIteration(
                    iteration,
                    fractional.Value.Name,
                    fractional.Value.Value,
                    $"{fractional.Value.Name} <= {Math.Floor(fractional.Value.Value)}",
                    simplexResult));

            currentModel =
                AddCut(
                    currentModel,
                    cut);
        }
    }
}
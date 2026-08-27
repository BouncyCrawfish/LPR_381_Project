using System;
using System.Collections.Generic;
using LPR381_Project.Models;

namespace LPR381_Project.Services
{
    public static class SensitivityAnalysisService
    {
        public static void CalculateNonBasicVariableRange(OptimizationResult result, int variableIndex, double originalCoefficient)
        {
            int objRow = result.FinalTableau.GetLength(0) - 1;
            double reducedCost = result.FinalTableau[objRow, variableIndex];
            double upperBound = originalCoefficient + reducedCost;

            Console.WriteLine($"\n--- Range for Non-Basic Variable X{variableIndex + 1} ---");
            Console.WriteLine($"Original Coefficient: {originalCoefficient}");
            Console.WriteLine($"Lower Bound: -Infinity");
            Console.WriteLine($"Upper Bound: {Math.Round(upperBound, 3)}");
        }

        public static void CalculateBasicVariableRange(OptimizationResult result, int variableIndex, double originalCoefficient)
        {
            int numRows = result.FinalTableau.GetLength(0);
            int numCols = result.FinalTableau.GetLength(1);
            int objRow = numRows - 1;

            int basicRow = -1;
            for (int i = 0; i < result.BasicVariables.Length; i++)
            {
                if (result.BasicVariables[i] == variableIndex)
                {
                    basicRow = i;
                    break;
                }
            }

            if (basicRow == -1) return;

            double maxIncrease = double.MaxValue;
            double maxDecrease = double.MaxValue;

            for (int j = 0; j < numCols - 1; j++)
            {
                if (IsBasicVariable(result.BasicVariables, j)) continue;

                double y_kj = result.FinalTableau[basicRow, j];
                double reducedCost = result.FinalTableau[objRow, j];

                if (y_kj < 0)
                {
                    double ratio = Math.Abs(reducedCost / y_kj);
                    if (ratio < maxIncrease) maxIncrease = ratio;
                }
                else if (y_kj > 0)
                {
                    double ratio = Math.Abs(reducedCost / y_kj);
                    if (ratio < maxDecrease) maxDecrease = ratio;
                }
            }

            double upperBound = maxIncrease == double.MaxValue ? double.PositiveInfinity : originalCoefficient + maxIncrease;
            double lowerBound = maxDecrease == double.MaxValue ? double.NegativeInfinity : originalCoefficient - maxDecrease;

            Console.WriteLine($"\n--- Range for Basic Variable X{variableIndex + 1} ---");
            Console.WriteLine($"Lower Bound: {(double.IsInfinity(lowerBound) ? "-Infinity" : Math.Round(lowerBound, 3))}");
            Console.WriteLine($"Upper Bound: {(double.IsInfinity(upperBound) ? "+Infinity" : Math.Round(upperBound, 3))}");
        }

        public static void CalculateRHSRange(OptimizationResult result, int constraintIndex, double originalRHS, int totalOriginalVariables)
        {
            int numRows = result.FinalTableau.GetLength(0);
            int numCols = result.FinalTableau.GetLength(1);
            int slackColumnIndex = totalOriginalVariables + constraintIndex;

            double maxIncrease = double.MaxValue;
            double maxDecrease = double.MaxValue;

            for (int i = 0; i < numRows - 1; i++)
            {
                double inverseValue = result.FinalTableau[i, slackColumnIndex];
                double currentRHS = result.FinalTableau[i, numCols - 1];

                if (inverseValue > 0)
                {
                    double ratio = currentRHS / inverseValue;
                    if (ratio < maxDecrease) maxDecrease = ratio;
                }
                else if (inverseValue < 0)
                {
                    double ratio = Math.Abs(currentRHS / inverseValue);
                    if (ratio < maxIncrease) maxIncrease = ratio;
                }
            }

            double upperBound = maxIncrease == double.MaxValue ? double.PositiveInfinity : originalRHS + maxIncrease;
            double lowerBound = maxDecrease == double.MaxValue ? double.NegativeInfinity : originalRHS - maxDecrease;

            Console.WriteLine($"\n--- Range for Constraint {constraintIndex + 1} RHS ---");
            Console.WriteLine($"Lower Bound: {(double.IsInfinity(lowerBound) ? "-Infinity" : Math.Round(lowerBound, 3))}");
            Console.WriteLine($"Upper Bound: {(double.IsInfinity(upperBound) ? "+Infinity" : Math.Round(upperBound, 3))}");
        }

        public static LinearProgram ApplyCoefficientChange(LinearProgram model, int variableIndex, double newCoefficient)
        {
            model.ObjectiveCoefficients[variableIndex] = newCoefficient;
            Console.WriteLine($"\nCoefficient for X{variableIndex + 1} changed to {newCoefficient}. Re-solve required.");
            return model;
        }

        public static LinearProgram ApplyRHSChange(LinearProgram model, int constraintIndex, double newRHS)
        {
            model.Constraints[constraintIndex].RightHandSide = newRHS;
            Console.WriteLine($"\nRHS for Constraint {constraintIndex + 1} changed to {newRHS}. Re-solve required.");
            return model;
        }

        public static LinearProgram ApplyNonBasicColumnChange(LinearProgram model, int variableIndex, List<double> newColumnValues)
        {
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                model.Constraints[i].Coefficients[variableIndex] = newColumnValues[i];
            }
            Console.WriteLine($"\nTechnological coefficients for X{variableIndex + 1} updated. Re-solve required.");
            return model;
        }

        public static LinearProgram AddNewActivity(LinearProgram model, double objCoefficient, List<double> columnCoefficients, string signRestriction)
        {
            model.ObjectiveCoefficients.Add(objCoefficient);
            model.SignRestrictions.Add(signRestriction);

            for (int i = 0; i < model.Constraints.Count; i++)
            {
                model.Constraints[i].Coefficients.Add(columnCoefficients[i]);
            }
            Console.WriteLine("\nNew activity (variable) added to the model. Re-solve required.");
            return model;
        }

        public static LinearProgram AddNewConstraint(LinearProgram model, List<double> coefficients, string relation, double rhs)
        {
            Constraint newConstraint = new Constraint
            {
                Coefficients = coefficients,
                Relation = relation,
                RightHandSide = rhs
            };
            model.Constraints.Add(newConstraint);
            Console.WriteLine("\nNew constraint added to the model. Re-solve using Dual Simplex recommended.");
            return model;
        }

        public static void DisplayShadowPrices(OptimizationResult result, int totalOriginalVariables, int totalConstraints)
        {
            int objRow = result.FinalTableau.GetLength(0) - 1;
            Console.WriteLine("\n--- Shadow Prices ---");

            for (int i = 0; i < totalConstraints; i++)
            {
                int slackColumnIndex = totalOriginalVariables + i;
                double shadowPrice = result.FinalTableau[objRow, slackColumnIndex];
                Console.WriteLine($"Constraint {i + 1}: {Math.Round(shadowPrice, 3)}");
            }
        }

        public static LinearProgram GenerateDualModel(LinearProgram primalModel)
        {
            LinearProgram dualModel = new LinearProgram();
            dualModel.OptimizationDirection = primalModel.OptimizationDirection == "max" ? "min" : "max";

            foreach (var constraint in primalModel.Constraints)
            {
                dualModel.ObjectiveCoefficients.Add(constraint.RightHandSide);
            }

            int numPrimalVars = primalModel.ObjectiveCoefficients.Count;
            for (int i = 0; i < numPrimalVars; i++)
            {
                Constraint dualConstraint = new Constraint();
                dualConstraint.RightHandSide = primalModel.ObjectiveCoefficients[i];
                dualConstraint.Relation = primalModel.OptimizationDirection == "max" ? ">=" : "<=";

                foreach (var primalConstraint in primalModel.Constraints)
                {
                    dualConstraint.Coefficients.Add(primalConstraint.Coefficients[i]);
                }
                dualModel.Constraints.Add(dualConstraint);
            }

            Console.WriteLine("\nDual model generated successfully.");
            return dualModel;
        }

        public static void VerifyDuality(double primalOptimalValue, double dualOptimalValue)
        {
            Console.WriteLine("\n--- Duality Verification ---");
            Console.WriteLine($"Primal Z = {Math.Round(primalOptimalValue, 3)}");
            Console.WriteLine($"Dual W = {Math.Round(dualOptimalValue, 3)}");

            if (Math.Abs(primalOptimalValue - dualOptimalValue) < 0.001)
            {
                Console.WriteLine("Result: Strong Duality Verified (Primal Z == Dual W).");
            }
            else
            {
                Console.WriteLine("Result: Weak Duality.");
            }
        }

        private static bool IsBasicVariable(int[] basicVariables, int colIndex)
        {
            foreach (int bv in basicVariables)
            {
                if (bv == colIndex) return true;
            }
            return false;
        }
    }
}

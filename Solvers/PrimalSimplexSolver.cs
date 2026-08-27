using System;
using System.Text;
using LPR381_Project.Models;
using LPR381_Project.Interfaces;

namespace LPR381_Project.Solvers
{
    public class PrimalSimplexSolver : ISolver
    {
        private double[,] _tableau;
        private int _numRows;
        private int _numCols;
        private int[] _basicVariables;

        public OptimizationResult Solve(LinearProgram model)
        {
            OptimizationResult result = new OptimizationResult();
            InitializeTableau(model);
            SaveTableauIteration(result, "Initial Canonical Form");

            while (!IsOptimal())
            {
                int enteringCol = GetEnteringVariable();
                int leavingRow = GetLeavingVariable(enteringCol);

                if (leavingRow == -1)
                    throw new InvalidOperationException("Model is unbounded.");

                Pivot(leavingRow, enteringCol);
                SaveTableauIteration(result, $"Pivot: Row {leavingRow}, Col {enteringCol}");
            }

            ExtractOptimalSolution(result);
            result.FinalTableau = _tableau;
            result.BasicVariables = _basicVariables;
            return result;
        }

        private void InitializeTableau(LinearProgram model)
        {
            _numRows = model.Constraints.Count + 1;
            _numCols = model.ObjectiveCoefficients.Count + 1;
            _tableau = new double[_numRows, _numCols];
            _basicVariables = new int[model.Constraints.Count];

            for (int i = 0; i < model.Constraints.Count; i++)
            {
                for (int j = 0; j < model.Constraints[i].Coefficients.Count; j++)
                {
                    _tableau[i, j] = model.Constraints[i].Coefficients[j];
                }
                _tableau[i, _numCols - 1] = model.Constraints[i].RightHandSide;
                _basicVariables[i] = (model.ObjectiveCoefficients.Count - model.Constraints.Count) + i;
            }

            int objRow = _numRows - 1;
            for (int j = 0; j < model.ObjectiveCoefficients.Count; j++)
            {
                _tableau[objRow, j] = -model.ObjectiveCoefficients[j];
            }
            _tableau[objRow, _numCols - 1] = 0.0;
        }

        private bool IsOptimal()
        {
            int objRow = _numRows - 1;
            for (int j = 0; j < _numCols - 1; j++)
            {
                if (_tableau[objRow, j] < 0) return false;
            }
            return true;
        }

        private int GetEnteringVariable()
        {
            int objRow = _numRows - 1;
            int enteringCol = 0;
            double mostNegative = 0;

            for (int j = 0; j < _numCols - 1; j++)
            {
                if (_tableau[objRow, j] < mostNegative)
                {
                    mostNegative = _tableau[objRow, j];
                    enteringCol = j;
                }
            }
            return enteringCol;
        }

        private int GetLeavingVariable(int enteringCol)
        {
            int leavingRow = -1;
            double minRatio = double.MaxValue;

            for (int i = 0; i < _numRows - 1; i++)
            {
                double coefficient = _tableau[i, enteringCol];
                if (coefficient > 0)
                {
                    double rhs = _tableau[i, _numCols - 1];
                    double ratio = rhs / coefficient;
                    if (ratio < minRatio)
                    {
                        minRatio = ratio;
                        leavingRow = i;
                    }
                }
            }
            return leavingRow;
        }

        private void Pivot(int leavingRow, int enteringCol)
        {
            double pivotElement = _tableau[leavingRow, enteringCol];
            for (int j = 0; j < _numCols; j++) _tableau[leavingRow, j] /= pivotElement;

            for (int i = 0; i < _numRows; i++)
            {
                if (i == leavingRow) continue;
                double multiplier = _tableau[i, enteringCol];
                for (int j = 0; j < _numCols; j++)
                {
                    _tableau[i, j] -= multiplier * _tableau[leavingRow, j];
                }
            }
            _basicVariables[leavingRow] = enteringCol;
        }

        private void ExtractOptimalSolution(OptimizationResult result)
        {
            int originalVarCount = _numCols - 1 - (_numRows - 1);
            result.OptimalVariables = new double[originalVarCount];

            for (int i = 0; i < _numRows - 1; i++)
            {
                int varIndex = _basicVariables[i];
                if (varIndex < originalVarCount)
                {
                    result.OptimalVariables[varIndex] = _tableau[i, _numCols - 1];
                }
            }
            result.OptimalValue = _tableau[_numRows - 1, _numCols - 1];
        }

        private void SaveTableauIteration(OptimizationResult result, string iterationTitle)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"--- {iterationTitle} ---");
            for (int i = 0; i < _numRows; i++)
            {
                for (int j = 0; j < _numCols; j++)
                {
                    sb.AppendFormat("{0,10:F3} | ", Math.Round(_tableau[i, j], 3));
                }
                sb.AppendLine();
            }
            result.TableauIterations.Add(sb.ToString());
        }
    }
}

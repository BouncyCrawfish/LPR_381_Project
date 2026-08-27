using System;
using System.Collections.Generic;

namespace LPSolver.Algorithms;

public static class RevisedPrimalSimplex
{
    private const double Epsilon = 1e-9;
    private const int MaxIterations = 1000;

    public static RevisedSimplexResult Solve(
    CanonicalForm canonicalForm)
    {
        var result =
            new RevisedSimplexResult();

        var basis =
            (string[])canonicalForm.BasisNames.Clone();

        int m = basis.Length;

        var bInverse =
            Identity(m);

        int iteration = 0;

        while (true)
        {
            if (iteration > MaxIterations)
            {
                throw new InvalidOperationException(
                    "Revised Primal Simplex failed to converge.");
            }

            string[] nonBasis =
                GetNonBasicVariables(
                    canonicalForm,
                    basis);

            double[] cb =
                GetBasisCosts(
                    canonicalForm,
                    basis);

            double[] cbBinv =
                MultiplyRow(
                    cb,
                    bInverse);

            Dictionary<string, double> reducedCosts =
                ComputeReducedCosts(
                    canonicalForm,
                    basis,
                    bInverse);

            int enteringColumn =
                SelectEnteringVariable(
                    canonicalForm,
                    reducedCosts);

            if (enteringColumn == -1)
            {
                result.Status =
                    SimplexStatus.Optimal;
                break;
            }

            string entering =
                canonicalForm.ColumnNames[
                    enteringColumn];

            double[] enteringVector =
                GetConstraintColumn(
                    canonicalForm.Matrix,
                    enteringColumn);

            double[] transformedColumn =
                Multiply(
                    bInverse,
                    enteringVector);

            double[] transformedRhs =
                Multiply(
                    bInverse,
                    GetConstraintRhs(
                        canonicalForm.Matrix));

            double[] theta =
                ComputeTheta(
                    transformedColumn,
                    transformedRhs);

            int leavingRow =
                SelectLeavingVariable(
                    theta);

            if (leavingRow == -1)
            {
                result.Status =
                    SimplexStatus.Unbounded;

                return result;
            }

            string leaving =
                basis[leavingRow];

            double[,] elementaryMatrix =
                BuildElementaryMatrix(
                    transformedColumn,
                    leavingRow);

            result.Iterations.Add(
                new RevisedSimplexIterationSnapshot(
                    iteration + 1,
                    (string[])basis.Clone(),
                    nonBasis,
                    (double[,])bInverse.Clone(),
                    (double[])cb.Clone(),
                    (double[])cbBinv.Clone(),
                    new Dictionary<string, double>(
                        reducedCosts),
                    entering,
                    leaving,
                    enteringVector,
                    transformedColumn,
                    theta,
                    transformedRhs,
                    elementaryMatrix));

            bInverse =
                Multiply(
                    elementaryMatrix,
                    bInverse);

            basis[leavingRow] =
                entering;

            iteration++;
        }

        result.ObjectiveValue =
            canonicalForm.ObjectiveWasNegated
                ? -ComputeObjectiveValue(
                    canonicalForm,
                    bInverse,
                    basis)
                : ComputeObjectiveValue(
                    canonicalForm,
                    bInverse,
                    basis);

        result.VariableValues =
            ExtractVariableValues(
                canonicalForm,
                bInverse,
                basis);

        return result;
    }

    private static double[,] Identity(int n)
    {
        var result = new double[n, n];

        for (int i = 0; i < n; i++)
            result[i, i] = 1.0;

        return result;
    }

    private static double Dot(double[] a, double[] b)
    {
        double sum = 0;

        for (int i = 0; i < a.Length; i++)
            sum += a[i] * b[i];

        return sum;
    }

    private static double[] Multiply(
    double[,] matrix,
    double[] vector)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        var result = new double[rows];

        for (int r = 0; r < rows; r++)
        {
            double sum = 0;

            for (int c = 0; c < cols; c++)
                sum += matrix[r, c] * vector[c];

            result[r] = sum;
        }

        return result;
    }

    private static double[,] Multiply(
    double[,] left,
    double[,] right)
    {
        int rows = left.GetLength(0);
        int cols = right.GetLength(1);
        int inner = left.GetLength(1);

        var result = new double[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                double sum = 0;

                for (int k = 0; k < inner; k++)
                    sum += left[r, k] * right[k, c];

                result[r, c] = sum;
            }
        }

        return result;
    }

    private static double[] GetConstraintColumn(
    double[,] matrix,
    int column)
    {
        int rows = matrix.GetLength(0) - 1;

        var result = new double[rows];

        for (int r = 1; r < matrix.GetLength(0); r++)
            result[r - 1] = matrix[r, column];

        return result;
    }

    private static double[] GetConstraintRhs(
    double[,] matrix)
    {
        int rows = matrix.GetLength(0) - 1;

        int rhsCol = matrix.GetLength(1) - 1;

        var result = new double[rows];

        for (int r = 1; r < matrix.GetLength(0); r++)
            result[r - 1] = matrix[r, rhsCol];

        return result;
    }

    private static double[,] BuildElementaryMatrix(
    double[] transformedColumn,
    int leavingRow)
    {
        int n = transformedColumn.Length;

        var E = Identity(n);

        double pivot = transformedColumn[leavingRow];

        for (int r = 0; r < n; r++)
        {
            if (r == leavingRow)
            {
                E[r, leavingRow] = 1.0 / pivot;
            }
            else
            {
                E[r, leavingRow] =
                    -transformedColumn[r] / pivot;
            }
        }

        return E;
    }

    private static string[] GetNonBasicVariables(
    CanonicalForm canonicalForm,
    string[] basis)
    {
        return canonicalForm.ColumnNames
            .Where(v => !basis.Contains(v))
            .ToArray();
    }

    private static double[] GetBasisCosts(
    CanonicalForm canonicalForm,
    string[] basis)
    {
        var costs = new double[basis.Length];

        for (int i = 0; i < basis.Length; i++)
        {
            int col =
                Array.IndexOf(
                    canonicalForm.ColumnNames,
                    basis[i]);

            costs[i] =
                -canonicalForm.Matrix[0, col];
        }

        return costs;
    }

    private static double[] MultiplyRow(
    double[] row,
    double[,] matrix)
    {
        int cols = matrix.GetLength(1);

        var result = new double[cols];

        for (int c = 0; c < cols; c++)
        {
            double sum = 0;

            for (int r = 0; r < row.Length; r++)
                sum += row[r] * matrix[r, c];

            result[c] = sum;
        }

        return result;
    }

    private static Dictionary<string, double>
ComputeReducedCosts(
    CanonicalForm canonicalForm,
    string[] basis,
    double[,] bInverse)
    {
        var reducedCosts =
            new Dictionary<string, double>();

        var nonBasic =
            GetNonBasicVariables(
                canonicalForm,
                basis);

        var cb =
            GetBasisCosts(
                canonicalForm,
                basis);

        var cbBinv =
            MultiplyRow(cb, bInverse);

        foreach (string variable in nonBasic)
        {
            int column =
                Array.IndexOf(
                    canonicalForm.ColumnNames,
                    variable);

            double[] aj =
                GetConstraintColumn(
                    canonicalForm.Matrix,
                    column);

            double ci =
                -canonicalForm.Matrix[0, column];

            double value =
                Dot(cbBinv, aj) - ci;

            reducedCosts[variable] = value;
        }

        return reducedCosts;
    }

    private static int SelectEnteringVariable(
    CanonicalForm canonicalForm,
    Dictionary<string, double> reducedCosts)
    {
        double best = -Epsilon;
        int entering = -1;

        for (int c = 0; c < canonicalForm.ColumnNames.Length; c++)
        {
            string variable =
                canonicalForm.ColumnNames[c];

            if (!reducedCosts.ContainsKey(variable))
                continue;

            double value =
                reducedCosts[variable];

            if (value < best)
            {
                best = value;
                entering = c;
            }
        }

        return entering;
    }

    private static double[] ComputeTheta(
    double[] transformedColumn,
    double[] transformedRhs)
    {
        var theta =
            new double[
                transformedColumn.Length];

        for (int i = 0; i < transformedColumn.Length; i++)
        {
            if (transformedColumn[i] <= Epsilon)
            {
                theta[i] =
                    double.PositiveInfinity;
            }
            else
            {
                theta[i] =
                    transformedRhs[i]
                    / transformedColumn[i];
            }
        }

        return theta;
    }

    private static int SelectLeavingVariable(
    double[] theta)
    {
        int leaving = -1;

        double best =
            double.PositiveInfinity;

        for (int i = 0; i < theta.Length; i++)
        {
            if (theta[i] < 0)
                continue;

            if (theta[i] == double.PositiveInfinity)
                continue;

            if (theta[i] < best - Epsilon)
            {
                best = theta[i];
                leaving = i;
            }
        }

        return leaving;
    }

    private static double ComputeObjectiveValue(
    CanonicalForm canonicalForm,
    double[,] bInverse,
    string[] basis)
    {
        double[] cb =
            GetBasisCosts(
                canonicalForm,
                basis);

        double[] cbBinv =
            MultiplyRow(
                cb,
                bInverse);

        double[] rhs =
            GetConstraintRhs(
                canonicalForm.Matrix);

        return Dot(cbBinv, rhs);
    }

    private static Dictionary<string, double>
ExtractVariableValues(
    CanonicalForm canonicalForm,
    double[,] bInverse,
    string[] basis)
    {
        var raw =
            canonicalForm.ColumnNames
                .ToDictionary(x => x, x => 0.0);

        double[] rhs =
            Multiply(
                bInverse,
                GetConstraintRhs(
                    canonicalForm.Matrix));

        for (int i = 0; i < basis.Length; i++)
            raw[basis[i]] = rhs[i];

        var final =
            new Dictionary<string, double>();

        foreach (var (_, expansion)
            in canonicalForm.VariableExpansions)
        {
            double value =
                expansion.IsNegatedSubstitution
                    ? -raw[expansion.PositivePartColumn]
                    : expansion.NegativePartColumn is null
                        ? raw[expansion.PositivePartColumn]
                        : raw[expansion.PositivePartColumn]
                          - raw[expansion.NegativePartColumn];

            final[expansion.OriginalName] =
                value;
        }

        return final;
    }


}
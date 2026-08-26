using System.Collections.Generic;

namespace LPSolver.Algorithms;

public class RevisedSimplexIterationSnapshot
{
	public int IterationNumber { get; }

	public string[] BasisVariables { get; }

	public string[] NonBasisVariables { get; }

	public double[,] BasisInverse { get; }

	public double[] BasisCosts { get; }

	public double[] CbvBInverse { get; }

	public Dictionary<string, double> ReducedCosts { get; }

	public string? EnteringVariable { get; }

	public string? LeavingVariable { get; }

	public double[]? EnteringColumn { get; }

	public double[]? TransformedColumn { get; }

	public double[]? ThetaValues { get; }

	public double[]? TransformedRhs { get; }

	public double[,]? ElementaryMatrix { get; }

	public RevisedSimplexIterationSnapshot(
		int iterationNumber,
		string[] basisVariables,
		string[] nonBasisVariables,
		double[,] basisInverse,
		double[] basisCosts,
		double[] cbvBInverse,
		Dictionary<string, double> reducedCosts,
		string? enteringVariable,
		string? leavingVariable,
		double[]? enteringColumn,
		double[]? transformedColumn,
		double[]? thetaValues,
		double[]? transformedRhs,
		double[,]? elementaryMatrix)
	{
		IterationNumber = iterationNumber;
		BasisVariables = basisVariables;
		NonBasisVariables = nonBasisVariables;
		BasisInverse = basisInverse;
		BasisCosts = basisCosts;
		CbvBInverse = cbvBInverse;
		ReducedCosts = reducedCosts;
		EnteringVariable = enteringVariable;
		LeavingVariable = leavingVariable;
		EnteringColumn = enteringColumn;
		TransformedColumn = transformedColumn;
		ThetaValues = thetaValues;
		TransformedRhs = transformedRhs;
		ElementaryMatrix = elementaryMatrix;
	}
}
using System.Globalization;
using System.Text.RegularExpressions;
using LPSolver.Models;

namespace LPSolver.IO;

/* 
    Reads the LP/IP input text file and produces
 an LPModel. The parser is intentionally strict: it throws a FormatException with a
 message the moment the file deviates from the required layout.

 Expected layout:
   Line 1:            max|min  <signed coeff> <signed coeff> ...
   Line 2..(n-1):      <signed coeff> ... <relation><rhs>      e.g. "+11 +8 <=40"
   Last line:          <sign restriction> ... one per decision variable
 */
public static class ModelParser
{
    // Matches a relation symbol glued directly to its RHS value, e.g. "<=40", ">=10", "=5.5"
    private static readonly Regex RelationRhsPattern = new(@"^(<=|>=|=)([+-]?\d+(\.\d+)?)$", RegexOptions.Compiled);

    public static LPModel Parse(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Input file not found: {filePath}");

        var lines = File.ReadAllLines(filePath)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();

        if (lines.Length < 3)
            throw new FormatException(
                "Input file must contain at least an objective line, one constraint line, and a sign-restriction line.");

        var (objective, objectiveCoefficients) = ParseObjectiveLine(lines[0]);

        var constraintLines = lines[1..^1];
        var constraints = constraintLines
            .Select(line => ParseConstraintLine(line, objectiveCoefficients.Length))
            .ToList();

        var signRestrictions = ParseSignRestrictionLine(lines[^1], objectiveCoefficients.Length);

        return new LPModel(objective, objectiveCoefficients, constraints, signRestrictions);
    }

    private static (ObjectiveType Objective, double[] Coefficients) ParseObjectiveLine(string line)
    {
        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2)
            throw new FormatException(
                $"Objective line must contain 'max'/'min' followed by at least one coefficient: \"{line}\"");

        ObjectiveType objective = tokens[0].ToLowerInvariant() switch
        {
            "max" => ObjectiveType.Maximize,
            "min" => ObjectiveType.Minimize,
            _ => throw new FormatException($"Expected 'max' or 'min' as the first token, got \"{tokens[0]}\"")
        };

        var coefficients = tokens[1..].Select(ParseSignedNumber).ToArray();

        return (objective, coefficients);
    }

    private static ConstraintModel ParseConstraintLine(string line, int expectedVariableCount)
    {
        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < expectedVariableCount + 1)
            throw new FormatException($"Constraint line has too few tokens: \"{line}\"");

        var relationToken = tokens[^1];
        var match = RelationRhsPattern.Match(relationToken);
        if (!match.Success)
            throw new FormatException($"Could not parse relation/RHS token \"{relationToken}\" in line: \"{line}\"");

        var relation = match.Groups[1].Value switch
        {
            "<=" => RelationType.LessThanOrEqual,
            ">=" => RelationType.GreaterThanOrEqual,
            "=" => RelationType.Equal,
            _ => throw new FormatException($"Unknown relation symbol in \"{relationToken}\"")
        };
        var rhs = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

        var coefficientTokens = tokens[..^1];
        if (coefficientTokens.Length != expectedVariableCount)
            throw new FormatException(
                $"Expected {expectedVariableCount} coefficients but found {coefficientTokens.Length} in line: \"{line}\"");

        var coefficients = coefficientTokens.Select(ParseSignedNumber).ToArray();

        return new ConstraintModel(coefficients, relation, rhs);
    }

    private static SignRestrictionType[] ParseSignRestrictionLine(string line, int expectedVariableCount)
    {
        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != expectedVariableCount)
            throw new FormatException(
                $"Expected {expectedVariableCount} sign restrictions but found {tokens.Length} in line: \"{line}\"");

        return tokens.Select(t => t.ToLowerInvariant() switch
        {
            "+" => SignRestrictionType.Positive,
            "-" => SignRestrictionType.Negative,
            "urs" => SignRestrictionType.Unrestricted,
            "int" => SignRestrictionType.Integer,
            "bin" => SignRestrictionType.Binary,
            _ => throw new FormatException($"Unknown sign restriction \"{t}\"")
        }).ToArray();
    }

    private static double ParseSignedNumber(string token)
    {
        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw new FormatException($"Could not parse numeric token \"{token}\"");
        return value;
    }
}

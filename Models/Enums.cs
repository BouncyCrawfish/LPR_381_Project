namespace LPSolver.Models;

//if the problem is max or min
public enum ObjectiveType
{
    Maximize,
    Minimize
}

/// The relation used on the rhs of a constraint.
public enum RelationType
{
    LessThanOrEqual,
    GreaterThanOrEqual,
    Equal
}

/*
 The sign restriction declared for a decision variable in the input file's last line:
 "+" (positive), "-" (negative), "urs" (unrestricted), "int" (integer), "bin" (binary).
 */
public enum SignRestrictionType
{
    Positive,
    Negative,
    Unrestricted,
    Integer,
    Binary
}

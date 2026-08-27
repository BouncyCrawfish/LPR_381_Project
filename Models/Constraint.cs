using System.Collections.Generic;

namespace LPR381_Project.Models
{
    public class Constraint
    {
        public List<double> Coefficients { get; set; } = new List<double>();
        public string Relation { get; set; }
        public double RightHandSide { get; set; }
    }
}

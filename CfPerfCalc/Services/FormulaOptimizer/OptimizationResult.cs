namespace CfPerfCalc.Services.FormulaOptimizer;

public class OptimizationResult
{
    public int ContestId { get; set; }
    public double ElitismValue { get; set; }
    public double SquareError { get; set; }
}
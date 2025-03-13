namespace SigGen.Models.Uniswap;

public class GasStrategy {
    public double DisplayLimitInflationFactor { get; set; } = 1.15;
    public double LimitInflationFactor { get; set; } = 1.15;
    public int MaxPriorityFeeGwei { get; set; } = 9;
    public int MinPriorityFeeGwei { get; set;} = 2;
    public int PercentileThresholdFor1559Fee { get; set;} = 75;
    public double PriceInflationFactor { get; set; } = 1.5;
}
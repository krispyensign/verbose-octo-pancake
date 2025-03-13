namespace SigGen.Models;

public class Quote {
    public int ChainId { get; set; }
    public TokenQuote? Input { get; set; }
    public TokenQuote? Output { get; set; }
    public string? Swapper { get; set; }
    public List<List<SwapRoute>>? Route { get; set; }
    public double Slippage { get; set; }
    public string? TradeType { get; set; }
    public string? QuoteId { get; set; }
    public string? GasFeeUSD { get; set; }
    public string? GasFeeQuote { get; set; }
    public string? GasUseEstimate { get; set; }
    public double PriceImpact { get; set; }
    public string? GasPrice { get; set; }
    public string? GasFee { get; set; }
    public List<GasEstimate>? GasEstimates { get; set; }
    public string? RouteString { get; set; }
    public string? BlockNumber { get; set; }
    public List<TokenQuote>? AggregatedOutputs { get; set; }
    public string? PortionAmount { get; set; }
    public int PortionBips { get; set; }
    public string? PortionRecipient { get; set; }
}

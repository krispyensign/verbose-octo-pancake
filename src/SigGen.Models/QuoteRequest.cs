namespace SigGen.Models;

public class QuoteRequest {
    public required string Amount { get; set; }
    public required SortedDictionary<int, GasStrategy> GasStrategies { get; set; }
    public List<string> Protocols { get; set; } = [
        "V4", "V3", "V2"
    ];
    public double SlippageTolerance { get; set; } = 2.5;
    public string Swapper { get; set; } = "0xAAAA44272dc658575Ba38f43C438447dDED45358";
    public required string TokenIn { get; set; }
    public int TokenInChainId { get; set; } = 8453;
    public required string TokenOut { get; set; }
    public int TokenOutChainId { get; set; } = 8453;
    public string Type { get; set; } = "EXACT_INPUT";
    public string Urgency { get; set; } = "normal";
}
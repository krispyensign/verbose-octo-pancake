namespace SigGen.Models.Uniswap;

public class QuoteResponse {
    public string? RequestId { get; set; }
    public string? Routing { get; set; }
    public Quote? Quote { get; set; }
}
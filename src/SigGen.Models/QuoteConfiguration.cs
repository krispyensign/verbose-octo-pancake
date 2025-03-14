namespace SigGen.Models;

public class QuoteConfiguration {
    public required string URL { get; set; }
    public required string BaseAddress { get; set; }
    public required string Path { get; set; }
    public required string QuoterAddress { get; set; }
    public string WalletAddress { get; set; }
    public required Dictionary<string, string> Tokens { get; set; }
    public required List<Pool> Pools { get; set; }
}
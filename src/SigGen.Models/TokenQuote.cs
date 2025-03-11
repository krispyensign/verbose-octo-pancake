namespace SigGen.Models;

public class TokenQuote {
    public string? Token { get; set; }
    public string? Amount { get; set; }
    public string? Recipient { get; set; }
    public int Bps { get; set; }
    public string? MinAmount { get; set; }    
}
namespace SigGen.Models;

public class SwapResponse {
    public string? RequestId { get; set; }
    public string? Routing { get; set; }
    public Quote? Quote { get; set; }
}
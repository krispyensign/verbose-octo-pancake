namespace SigGen.Models;

public class SwapRoute {
    public string? Type { get; set; }
    public string? Address { get; set; }
    public TokenInfo? TokenIn { get; set; }
    public TokenInfo? TokenOut { get; set; }
    public string? Fee { get; set; }
    public string? Liquidity { get; set; }
    public string? SqrtRatioX96 { get; set; }
    public string? TickCurrent { get; set; }
    public string? AmountIn { get; set; }
    public string? AmountOut { get; set; }
}
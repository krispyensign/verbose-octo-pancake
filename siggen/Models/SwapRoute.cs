namespace SigGen.Models;

public class SwapRoute {
    string Type { get; set; }
    string Address { get; set; }
    TokenInfo TokenIn { get; set; }
    TokenInfo TokenOut { get; set; }
    string Fee { get; set; }
    string Liquidity { get; set; }
    string SqrtRatioX96 { get; set; }
    string TickCurrent { get; set; }
    string AmountIn { get; set; }
    string AmountOut { get; set; }
}
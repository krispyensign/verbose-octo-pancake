using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace SigGen.Models.Uniswap;

// Define the function parameters
public class QuoteExactInputSingleParams
{
    [Parameter("address", "tokenIn", 1)]
    public string TokenIn { get; set; }
    [Parameter("address", "tokenOut", 2)]
    public string TokenOut { get; set; }
    [Parameter("uint256", "amountIn", 3)]
    public BigInteger AmountIn { get; set; }
    [Parameter("uint24", "fee", 4)]
    public uint Fee { get; set; }
    [Parameter("uint160", "sqrtPriceLimitX96", 5)]
    public BigInteger SqrtPriceLimitX96 { get; set; }
}

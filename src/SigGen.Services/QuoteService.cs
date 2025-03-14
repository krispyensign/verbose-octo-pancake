using System.Numerics;
using SigGen.Models;

namespace SigGen.Services;

public interface IQuoteService
{
    Task<string> GetQuote(
        string amount, string tokenIn, string tokenOut,
        CancellationToken cancellationToken = default);
    Task<BigInteger> GetExactQuoteV4(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta);
}

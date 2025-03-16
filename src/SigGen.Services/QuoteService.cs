using System.Numerics;
using SigGen.Models;

namespace SigGen.Services;

public interface IQuoteService
{
    Task<string> GetQuote(
        string amount, string tokenIn, string tokenOut,
        CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetValueQuotes(Dictionary<string, BigInteger> balances, string startingToken);
}

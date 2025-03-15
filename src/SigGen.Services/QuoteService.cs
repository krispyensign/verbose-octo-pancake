using System.Numerics;
using SigGen.Models;

namespace SigGen.Services;

public interface IQuoteService
{
    Task<BigInteger> GetExactQuoteV4(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta);
    Task<BigInteger> GetExactQuoteV2(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta);
    Task<Dictionary<string, string>> GetValueQuotes(Dictionary<string, BigInteger> balances, string startingToken);
}

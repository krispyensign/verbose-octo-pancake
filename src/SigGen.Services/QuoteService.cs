using System.Numerics;
using SigGen.Models;

namespace SigGen.Services;

public interface IQuoteService
{
    Task<BigInteger> GetExactQuoteV4(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta);
    Task<List<(string, BigInteger)>> GetExactQuoteV2(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta);
    Task<Dictionary<string, List<BigInteger>>> GetValueQuotes(string baseAsset, string quoteAsset);
}

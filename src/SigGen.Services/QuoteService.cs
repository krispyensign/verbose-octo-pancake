using System.Numerics;
using Nethereum.Util;
using SigGen.Models;

namespace SigGen.Services;

public interface IQuoteService
{
    Task<List<(string, BigDecimal)>> GetExactQuoteV4(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta);
    Task<List<(string, BigDecimal)>> GetExactQuoteV2(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta);
    Task<List<(string, BigDecimal)>> GetValueQuotes(string baseAsset, string quoteAsset);
    Task<BigDecimal> GetGasPrice();
}

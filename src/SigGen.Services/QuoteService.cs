using System.Numerics;
using Nethereum.Util;
using SigGen.Models;

namespace SigGen.Services;

public interface IQuoteService
{
    Task<BigInteger> GetExactQuoteV4(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta);
    Task<BigInteger> GetExactQuoteV2(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta);
    Task<BigInteger> GetGasPrice();
}

using Microsoft.Extensions.Logging;
using SigGen.Models;
using Nethereum.Web3;
using System.Numerics;
using Nethereum.Uniswap.V4.V4Quoter;
using Nethereum.Uniswap.V4.V4Quoter.ContractDefinition;
using Nethereum.Uniswap;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using System.Drawing;
using Nethereum.Uniswap.V3.QuoterV2;
using Nethereum.ABI;
using Nethereum.Uniswap.V3.QuoterV2.ContractDefinition;

namespace SigGen.Services;

public class UniswapQuoteService : IQuoteService
{
    private readonly QuoteConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly Web3 web3;
    private static readonly string zero = "0x0000000000000000000000000000000000000000";

    public UniswapQuoteService(QuoteConfiguration configuration, ILogger<UniswapQuoteService> logger)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _logger = logger 
            ?? throw new ArgumentNullException(nameof(logger));

        web3 = new Web3(_configuration.URL);
    }


    public async Task<BigInteger> GetExactQuoteV4(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta)
    {
        var pool = _configuration.Pools.FirstOrDefault(p => p.Meta.Contains(meta) && (
            p.Token0Name == tokenSymbolIn || p.Token0Name == tokenSymbolOut
        ) && (
            p.Token1Name == tokenSymbolIn || p.Token1Name == tokenSymbolOut
        )) ?? throw new InvalidOperationException("bad tokens");

        var pk = new PoolKey
        {
            Currency0 = pool.Token0Name,
            Currency1 = pool.Token1Name,
            Fee = pool.Fee,
            TickSpacing = pool.TickSpacing,
            Hooks = zero,
        };

        var v4Quoter = new V4QuoterService(web3, UniswapAddresses.BaseQuoterV4);

        var quoteExactParams = new QuoteExactSingleParams()
        {
            ExactAmount = amountIn,
            PoolKey = pk,
            ZeroForOne = false,
            HookData = zero.HexToByteArray(),
        };

        var quote = await v4Quoter.QuoteExactInputSingleQueryAsync(quoteExactParams);

        return quote.AmountOut;
    }

    public async Task<BigInteger> GetExactQuoteV2(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta)
    {

        var pool = _configuration.Pools.FirstOrDefault(p => p.Meta.Contains(meta) && (
            p.Token0Name == tokenSymbolIn || p.Token0Name == tokenSymbolOut
        ) && (
            p.Token1Name == tokenSymbolIn || p.Token1Name == tokenSymbolOut
        )) ?? throw new InvalidOperationException("bad tokens");
        var tokenIn = _configuration.Tokens[tokenSymbolIn];
        var tokenOut = _configuration.Tokens[tokenSymbolOut];

        var quoterService = new QuoterV2Service(web3, UniswapAddresses.BaseQuoterV2);
        var weth9 = await quoterService.Weth9QueryAsync();
        var abiEncoder = new ABIEncode();

        var parms = new QuoteExactInputSingleParams
        {
            AmountIn = amountIn,
            Fee = pool.Fee,
            TokenIn = tokenIn,
            TokenOut = tokenOut,
        };

        var quote = await quoterService.QuoteExactInputSingleQueryAsync(parms);

        return quote.AmountOut;
    }
}

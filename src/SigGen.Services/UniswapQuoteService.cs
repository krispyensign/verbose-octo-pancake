using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SigGen.Models;
using Nethereum.Web3;
using System.Numerics;
using System.ComponentModel;
using Nethereum.Uniswap.V4.V4Quoter;
using Nethereum.Uniswap.V4.V4Quoter.ContractDefinition;
using Nethereum.Uniswap.V4.PositionManager;
using Nethereum.Uniswap.V4;
using Nethereum.Uniswap.V4.StateView;
using Nethereum.Uniswap;
using Nethereum.Hex.HexConvertors.Extensions;

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
        var pool = _configuration.Pools.FirstOrDefault(p => p.Meta == meta && (
            p.Token0Name == tokenSymbolIn || p.Token0Name == tokenSymbolOut
        ) && (
            p.Token1Name == tokenSymbolIn || p.Token1Name == tokenSymbolOut
        )) ?? throw new InvalidOperationException("bad tokens");
        var tokenIn = _configuration.Tokens[tokenSymbolIn];
        var tokenOut = _configuration.Tokens[tokenSymbolOut];
        var pk = new PoolKey
        {
            Currency0 = zero,
            Currency1 = tokenIn,
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
}

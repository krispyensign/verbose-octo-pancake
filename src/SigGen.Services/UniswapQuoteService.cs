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
using Microsoft.VisualBasic;
using Nethereum.RPC.TransactionReceipts;

namespace SigGen.Services;

public class UniswapQuoteService : IQuoteService
{
    private readonly QuoteConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly Web3 web3;
    private static readonly string zero = "0x0000000000000000000000000000000000000000";
    private readonly V4QuoterService _v4QuoterService;
    private readonly QuoterV2Service _quoterV2Service;

    public UniswapQuoteService(QuoteConfiguration configuration, ILogger<UniswapQuoteService> logger)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        web3 = new Web3(_configuration.URL);

        _v4QuoterService = new V4QuoterService(web3, UniswapAddresses.BaseQuoterV4);
        _quoterV2Service = new QuoterV2Service(web3, UniswapAddresses.BaseQuoterV2);
    }

    public async Task<BigInteger> GetExactQuoteV4(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta)
    {
        var pool = GetPool(tokenSymbolIn, tokenSymbolOut, meta);
        if (pool == null)
        {
            return 0;
        }

        var quoteExactParams = new QuoteExactSingleParams()
        {
            ExactAmount = amountIn,
            PoolKey = new PoolKey
            {
                Currency0 = _configuration.Tokens[pool.Token0Name],
                Currency1 = _configuration.Tokens[pool.Token1Name],
                Fee = pool.Fee,
                TickSpacing = pool.TickSpacing,
                Hooks = zero,
            },
            ZeroForOne = pool.Token0Name == tokenSymbolIn,
            HookData = zero.HexToByteArray(),
        };
        var quote = await _v4QuoterService.QuoteExactInputSingleQueryAsync(quoteExactParams);

        return quote.AmountOut;
    }

    public async Task<BigInteger> GetExactQuoteV2(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta)
    {
        var pool = GetPool(tokenSymbolIn, tokenSymbolOut, meta);
        if (pool == null)
        {
            return 0;
        }
        WrapEthIfEth(ref tokenSymbolIn, ref tokenSymbolOut);

        var parms = new QuoteExactInputSingleParams
        {
            AmountIn = amountIn,
            Fee = pool.Fee,
            TokenIn = _configuration.Tokens[tokenSymbolIn],
            TokenOut = _configuration.Tokens[tokenSymbolOut],
        };
        var quote = await _quoterV2Service.QuoteExactInputSingleQueryAsync(parms);

        return quote.AmountOut;
    }

    private Pool? GetPool(string tokenSymbolIn, string tokenSymbolOut, string meta)
    {
        return _configuration.Pools.FirstOrDefault(p => p?.Meta?.Contains(meta) ?? true && (
            (p?.Token0Name ?? "") == tokenSymbolIn || (p?.Token0Name ?? "") == tokenSymbolOut
        ) && (
            (p?.Token1Name ?? "") == tokenSymbolIn || (p?.Token1Name ?? "") == tokenSymbolOut
        ));
    }

    private static void WrapEthIfEth(ref string tokenSymbolIn, ref string tokenSymbolOut)
    {
        if (tokenSymbolIn == "ETH")
        {
            tokenSymbolIn = "WETH";
        }
        if (tokenSymbolOut == "ETH")
        {
            tokenSymbolOut = "WETH";
        }
    }

    public async Task<Dictionary<string, List<BigInteger>>> GetValueQuotes(string tokenIn0, string tokenIn1, BigInteger amount)
    {
        var results = new Dictionary<string, List<BigInteger>>();

        try
        {
            var price1 = await GetExactQuoteV2(amount, tokenIn0, tokenIn1, "v3");
            var price2 = await GetExactQuoteV2(amount, tokenIn1, tokenIn0, "v3");
            BigInteger bid, ask;
            if (price1 > price2)
            {
                ask = price1;
                bid = price2;
            }
            else
            {
                ask = price2;
                bid = price1;
            }

            results.Add("v3", [bid, ask]);
        }
        catch (Exception e)
        {
            _logger.LogError("{}", e);
        }

        try
        {
            var price1 = await GetExactQuoteV4(amount, tokenIn0, tokenIn1, "v4");
            var price2 = await GetExactQuoteV4(amount, tokenIn1, tokenIn0, "v4");
            BigInteger bid, ask;
            if (price1 > price2)
            {
                ask = price1;
                bid = price2;
            }
            else
            {
                ask = price2;
                bid = price1;
            }
            results.Add("v4", [bid, ask]);
        }
        catch (Exception e)
        {
            _logger.LogError("{}", e);
        }

        return results;
    }
}

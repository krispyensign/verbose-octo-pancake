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

    public async Task<List<(string, BigInteger)>> GetExactQuoteV2(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta)
    {
        var pool = GetPool(tokenSymbolIn, tokenSymbolOut, meta);
        if (pool == null)
        {
            return [];
        }
        WrapEthIfEth(ref tokenSymbolIn, ref tokenSymbolOut);

        var results = new List<(string, BigInteger)>();

        // Given - calculates BID of DRB/ETH
        // In = DRB
        // Out = ETH
        // Amount = 1 WEI
        // Result = ETHWEI per 1 DRBWEI
        var parms0 = new QuoteExactInputSingleParams
        {
            AmountIn = amountIn,
            Fee = pool.Fee,
            TokenIn = _configuration.Tokens[tokenSymbolIn],
            TokenOut = _configuration.Tokens[tokenSymbolOut],
        };
        var quote0 = await _quoterV2Service.QuoteExactInputSingleQueryAsync(parms0);
        results.Add(($"{tokenSymbolIn}/{tokenSymbolOut}-v2-bid", quote0.AmountOut));

        // Given - calculates ASK of DRB/ETH
        // In = ETH
        // Out = DRB
        // Amount = 1 DRBWEI
        // Result = ETHWEI per 1 DRBWEI
        var parms3 = new QuoteExactOutputSingleParams
        {
            Amount = amountIn,
            Fee = pool.Fee,
            TokenIn = _configuration.Tokens[tokenSymbolOut],
            TokenOut = _configuration.Tokens[tokenSymbolIn],
        };
        var quote3 = await _quoterV2Service.QuoteExactOutputSingleQueryAsync(parms3);
        results.Add(($"{tokenSymbolIn}/{tokenSymbolOut}-v2-ask", quote3.AmountIn));

        // Given - calculates BID of ETH/DRB
        // In = ETH
        // Out = DRB
        // Amount = 1 WEI
        // Result = DRBWEI per 1 ETHWEI
        var parms1 = new QuoteExactInputSingleParams
        {
            AmountIn = amountIn,
            Fee = pool.Fee,
            TokenIn = _configuration.Tokens[tokenSymbolOut],
            TokenOut = _configuration.Tokens[tokenSymbolIn],
        };
        var quote1 = await _quoterV2Service.QuoteExactInputSingleQueryAsync(parms1);
        results.Add(($"{tokenSymbolOut}/{tokenSymbolIn}-v2-bid", quote1.AmountOut));

        // Given - calculates ASK of ETH/DRB
        // In = DRB
        // Out = ETH
        // Amount = 1 WEI
        // Result = DRBWEI per 1 ETHWEI
        var parms2 = new QuoteExactOutputSingleParams
        {
            Amount = amountIn,
            Fee = pool.Fee,
            TokenIn = _configuration.Tokens[tokenSymbolIn],
            TokenOut = _configuration.Tokens[tokenSymbolOut],
        };
        var quote2 = await _quoterV2Service.QuoteExactOutputSingleQueryAsync(parms2);
        results.Add(($"{tokenSymbolOut}/{tokenSymbolIn}-v2-ask", quote2.AmountIn));

        return results;
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

    public async Task<Dictionary<string, List<BigInteger>>> GetValueQuotes(string baseAsset, string quoteAsset)
    {
        var results = new Dictionary<string, List<BigInteger>>();

        try
        {
            var prices = await GetExactQuoteV2(1000000000000000000, baseAsset, quoteAsset, "v3");
        }
        catch (Exception e)
        {
            _logger.LogError("{}", e);
        }

        try
        {
            var price1 = await GetExactQuoteV4(1, baseAsset, quoteAsset, "v4");
            var price2 = await GetExactQuoteV4(1, quoteAsset, baseAsset, "v4");
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

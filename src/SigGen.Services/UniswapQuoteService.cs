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
using Nethereum.Util;

namespace SigGen.Services;

public class UniswapQuoteService : IQuoteService
{
    private readonly QuoteConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly Web3 web3;
    private static readonly string zero = "0x0000000000000000000000000000000000000000";
    private readonly V4QuoterService _v4QuoterService;
    private readonly QuoterV2Service _quoterV2Service;
    private static readonly decimal unit = 1000000000000000000M;

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

    public async Task<List<(string, BigDecimal)>> GetExactQuoteV4(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta)
    {
        var pool = GetPool(tokenSymbolIn, tokenSymbolOut, meta);
        if (pool == null)
        {
            return [];
        }
        var poolKey = new PoolKey
        {
            Currency0 = _configuration.Tokens[pool.Token0Name],
            Currency1 = _configuration.Tokens[pool.Token1Name],
            Fee = pool.Fee,
            TickSpacing = pool.TickSpacing,
            Hooks = zero,
        };

        var results = new List<(string, BigDecimal)>();

        // Given - calculates BID of DRB/ETH
        // In = DRB
        // Out = ETH
        // Amount = 1 WEI
        // Result = ETHWEI per 1 DRBWEI
        var parms0 = new QuoteExactSingleParams
        {
            ExactAmount = amountIn,
            PoolKey = poolKey,
            ZeroForOne = true,
            HookData = zero.HexToByteArray(),
        };
        var quote0 = await _v4QuoterService.QuoteExactInputSingleQueryAsync(parms0);
        var q0 = new BigDecimal(quote0.AmountOut)/unit;
        results.Add(($"{tokenSymbolOut}/{tokenSymbolIn}-v4-bid", q0));

        // Given - calculates ASK of DRB/ETH
        // In = ETH
        // Out = DRB
        // Amount = 1 DRBWEI
        // Result = ETHWEI per 1 DRBWEI
        var parms1 = new QuoteExactSingleParams
        {
            ExactAmount = amountIn,
            PoolKey = poolKey,
            ZeroForOne = false,
            HookData = zero.HexToByteArray(),
        };
        var quote1 = await _v4QuoterService.QuoteExactOutputSingleQueryAsync(parms1);
        var q1 = new BigDecimal(quote1.AmountIn)/unit;
        results.Add(($"{tokenSymbolOut}/{tokenSymbolIn}-v4-ask", q1));

        // Given - calculates BID of ETH/DRB
        // In = ETH
        // Out = DRB
        // Amount = 1 WEI
        // Result = DRBWEI per 1 ETHWEI
        var parms2 = new QuoteExactSingleParams
        {
            ExactAmount = amountIn,
            PoolKey = poolKey,
            ZeroForOne = false,
            HookData = zero.HexToByteArray(),
        };
        var quote2 = await _v4QuoterService.QuoteExactInputSingleQueryAsync(parms2);
        var q2 = new BigDecimal(quote2.AmountOut)/unit;
        results.Add(($"{tokenSymbolIn}/{tokenSymbolOut}-v4-bid", q2));

        // Given - calculates ASK of ETH/DRB
        // In = DRB
        // Out = ETH
        // Amount = 1 ETHWEI
        // Result = DRBWEI per 1 ETHWEI
        var parms3 = new QuoteExactSingleParams
        {
            ExactAmount = amountIn,
            PoolKey = poolKey,
            ZeroForOne = true,
            HookData = zero.HexToByteArray(),
        };
        var quote3 = await _v4QuoterService.QuoteExactOutputSingleQueryAsync(parms3);
        var q3 = new BigDecimal(quote3.AmountIn)/unit;
        results.Add(($"{tokenSymbolIn}/{tokenSymbolOut}-v4-ask", q3));
        return results;
    }

    public async Task<List<(string, BigDecimal)>> GetExactQuoteV2(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta)
    {
        var pool = GetPool(tokenSymbolIn, tokenSymbolOut, meta);
        if (pool == null)
        {
            return [];
        }
        WrapEthIfEth(ref tokenSymbolIn, ref tokenSymbolOut);

        var results = new List<(string, BigDecimal)>();

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
        var q0 = new BigDecimal(quote0.AmountOut)/unit;
        results.Add(($"{tokenSymbolIn}/{tokenSymbolOut}-v2-bid", q0));

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
        var q3 = new BigDecimal(quote3.AmountIn)/unit;
        results.Add(($"{tokenSymbolIn}/{tokenSymbolOut}-v2-ask", q3));

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
        var q1 = new BigDecimal(quote1.AmountOut)/unit;
        results.Add(($"{tokenSymbolOut}/{tokenSymbolIn}-v2-bid", q1));

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
        var q2 = new BigDecimal(quote2.AmountIn)/unit;
        results.Add(($"{tokenSymbolOut}/{tokenSymbolIn}-v2-ask", q2));

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

    public async Task<BigDecimal> GetGasPrice() 
    {
        var gasPrice = await web3.Eth.GasPrice.SendRequestAsync();
        var g = new BigDecimal(gasPrice.Value) / unit;
        return g;
    }

    public async Task<List<(string, BigDecimal)>> GetValueQuotes(string baseAsset, string quoteAsset)
    {
        var results = new List<(string, BigDecimal)>();

        try
        {
            var prices = await GetExactQuoteV2((BigInteger)unit, baseAsset, quoteAsset, "v3");
            results.AddRange(prices);
        }
        catch (Exception e)
        {
            _logger.LogError("{}", e);
        }

        try
        {
            var prices = await GetExactQuoteV4((BigInteger)unit, baseAsset, quoteAsset, "v4");
            results.AddRange(prices);
        }
        catch (Exception e)
        {
            _logger.LogError("{}", e);
        }

        return results;
    }
}

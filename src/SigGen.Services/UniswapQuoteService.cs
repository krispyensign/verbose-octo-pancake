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
        if (amountIn <= 0)
        {
            return 0;
        }
        var pool = GetPool(tokenSymbolIn, tokenSymbolOut, meta);
        if (pool == null)
        {
            _logger.LogError("Failed to retrieve pool");
            throw new ApplicationException("Failed to retrieve pool");
        }
        var poolKey = new PoolKey
        {
            Currency0 = _configuration.Tokens[pool.Token0Name],
            Currency1 = _configuration.Tokens[pool.Token1Name],
            Fee = pool.Fee,
            TickSpacing = pool.TickSpacing,
            Hooks = zero,
        };

        var parms0 = new QuoteExactSingleParams
        {
            ExactAmount = amountIn,
            PoolKey = poolKey,
            ZeroForOne = tokenSymbolIn == pool.Token0Name,
            HookData = zero.HexToByteArray(),
        };
        var quote0 = await _v4QuoterService.QuoteExactInputSingleQueryAsync(parms0);
        if (quote0 == null)
        {
            _logger.LogError("Failed to retrieve quote");
            throw new ApplicationException("Failed to retrieve quote");
        }

        return quote0.AmountOut;
    }

    public async Task<BigInteger> GetExactQuoteV2(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta)
    {
        if (amountIn <= 0)
        {
            return 0;
        }
        var pool = GetPool(tokenSymbolIn, tokenSymbolOut, meta);
        if (pool == null)
        {
            _logger.LogError("Failed to retrieve pool");
            throw new ApplicationException("Failed to retrieve pool");
        }
        WrapEthIfEth(ref tokenSymbolIn, ref tokenSymbolOut);

        var parms0 = new QuoteExactInputSingleParams
        {
            AmountIn = amountIn,
            Fee = pool.Fee,
            TokenIn = _configuration.Tokens[tokenSymbolIn],
            TokenOut = _configuration.Tokens[tokenSymbolOut],
        };
        var quote0 = await _quoterV2Service.QuoteExactInputSingleQueryAsync(parms0);
        if (quote0 == null)
        {
            _logger.LogError("Failed to retrieve quote");
            throw new ApplicationException("Failed to retrieve quote");
        }

        return quote0.AmountOut;
    }

    private Pool? GetPool(string tokenSymbolIn, string tokenSymbolOut, string meta)
    {
        List<string> set1 = [tokenSymbolIn, tokenSymbolOut];
        return _configuration.Pools.Where(p => 
                p.Meta == meta)
            .Where(p =>
                set1.Contains(p.Token0Name))
            .Where(p =>
                set1.Contains(p.Token1Name))
            .First();
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

    public async Task<BigInteger> GetGasPrice() 
    {
        return await web3.Eth.GasPrice.SendRequestAsync();
    }
}

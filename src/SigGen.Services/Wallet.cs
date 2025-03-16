using System.Numerics;
using Nethereum.Web3;
using SigGen.Models;
using Microsoft.Extensions.Logging;
using Nethereum.StandardTokenEIP20;

namespace SigGen.Services;


public interface IWalletService
{
    Task<Dictionary<string, BigInteger>> GetTokenBalances();
}


public class WalletService : IWalletService
{
    private readonly QuoteConfiguration _configuration;
    private readonly ILogger _logger;
    // ERC20 Token Contract ABI (simplified)
    private Dictionary<string, BigInteger> tokenDecimals = [];
    private readonly Web3 web3;

    public WalletService(QuoteConfiguration configuration, ILogger<WalletService> logger)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _logger = logger 
            ?? throw new ArgumentNullException(nameof(logger));

        web3 = new Web3(_configuration.URL);
    }

    public static void Upsert(Dictionary<string, BigInteger> dict, string key, BigInteger data)
    {
        if (!dict.TryAdd(key, data))
        {
            dict[key] = data;
        }
    }

    public async Task<Dictionary<string, BigInteger>> GetTokenBalances()
    {
        var tokenBalances = new Dictionary<string, BigInteger>{};
        foreach (var tokenAddress in _configuration.Tokens)
        {
            BigInteger tokens;
            if (tokenAddress.Key == "ETH")
            {
                try {
                    tokens = await web3.Eth.GetBalance.SendRequestAsync(_configuration.WalletAddress);
                }
                catch (Exception e)
                {
                    _logger.LogError("{}", e);
                    continue;
                }
                Upsert(tokenDecimals, tokenAddress.Key, 18);
            } else {
                var tokenService = new StandardTokenService(web3, tokenAddress.Value);
                try {
                    tokens = await tokenService.BalanceOfQueryAsync(_configuration.WalletAddress);
                }
                catch (Exception e)
                {
                    _logger.LogError("{}", e);
                    continue;
                }
                var decimals = await tokenService.DecimalsQueryAsync();
                Upsert(tokenDecimals, tokenAddress.Key, decimals);
            }

            Upsert(tokenBalances, tokenAddress.Key, tokens);
        }

        return tokenBalances;
    }
}
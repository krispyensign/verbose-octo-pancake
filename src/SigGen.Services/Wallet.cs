using System.Numerics;
using Nethereum.Web3;
using SigGen.Models;
using Microsoft.Extensions.Logging;
using Nethereum.StandardTokenEIP20;

namespace SigGen.Services;


public interface IWalletService
{
    decimal ConvertFromWei(string tokenAddress);
    Task<Dictionary<string, BigInteger>> GetTokenBalances();
}


public class WalletService : IWalletService
{
    private readonly QuoteConfiguration _configuration;
    private readonly ILogger _logger;
    // ERC20 Token Contract ABI (simplified)
    private Dictionary<string, uint> tokenDecimals = [];
    private readonly Web3 web3;

    public WalletService(QuoteConfiguration configuration, ILogger<WalletService> logger)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _logger = logger 
            ?? throw new ArgumentNullException(nameof(logger));

        web3 = new Web3(_configuration.URL);
    }

    public decimal ConvertFromWei(string tokenAddress)
    {
        if (tokenDecimals.TryGetValue(tokenAddress, out var decimals)) {
            return decimals;
        }

        return 0;
    }

    public async Task<Dictionary<string, BigInteger>> GetTokenBalances()
    {
        var tokenBalances = new Dictionary<string, BigInteger>{};
        foreach (var tokenAddress in _configuration.Tokens)
        {
            BigInteger tokens;
            if (tokenAddress.Key == "ETH")
            {
                tokens = await web3.Eth.GetBalance.SendRequestAsync(_configuration.WalletAddress);
            } else {
                var tokenService = new StandardTokenService(web3, tokenAddress.Value);
                tokens = await tokenService.BalanceOfQueryAsync(_configuration.WalletAddress);
            }

            tokenBalances.Add(tokenAddress.Key, tokens);
        }

        return tokenBalances;
    }
}
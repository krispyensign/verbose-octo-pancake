using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Nethereum.Web3;
using SigGen.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SigGen.Services;


public interface IWalletService
{
    decimal ConvertFromWei(string tokenAddress);
    Task<List<BigInteger>> GetTokenBalances(List<string> tokenAddresses);
}


public class WalletService : IWalletService
{
    private readonly WalletConfiguration _configuration;
    private readonly ILogger _logger;
    // ERC20 Token Contract ABI (simplified)
    private static readonly string abi = @"[
        { 'constant': true, 'inputs': [{ 'name': 'owner', 'type': 'address' }], 'name': 'balanceOf', 'outputs': [{ 'name': '', 'type': 'uint256' }], 'payable': false, 'stateMutability': 'view', 'type': 'function' },
        { 'constant': true, 'inputs': [], 'name': 'decimals', 'outputs': [{ 'name': '', 'type': 'uint8' }], 'payable': false, 'stateMutability': 'view', 'type': 'function' }
    ]";
    private Dictionary<string, uint> tokenDecimals = [];
    private readonly Web3 web3;

    public WalletService(WalletConfiguration configuration, ILogger<WalletService> logger)
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

    public async Task<List<BigInteger>> GetTokenBalances(List<string> tokenAddresses)
    {
        _logger.LogInformation("using base Address {}", _configuration.URL);
        var tokenBalances = new List<BigInteger>{};
        foreach (var tokenAddress in tokenAddresses)
        {
            var contract = web3.Eth.GetContract(abi, tokenAddress);
            var balanceFunction = contract.GetFunction("balanceOf");
            var decimalsFunction = contract.GetFunction("decimals");

            // Get the balance
            var balance = await balanceFunction.CallAsync<BigInteger>(_configuration.WalletAddress);
            var decimals = await decimalsFunction.CallAsync<uint>();
            tokenDecimals.Add(tokenAddress, decimals);
            tokenBalances.Add(balance);
        }

        return tokenBalances;
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Nethereum.Web3;

namespace SigGen.Services;

public class WalletService {

    // ERC20 Token Contract ABI (simplified)
    private static readonly string abi = @"[
        { 'constant': true, 'inputs': [{ 'name': 'owner', 'type': 'address' }], 'name': 'balanceOf', 'outputs': [{ 'name': '', 'type': 'uint256' }], 'payable': false, 'stateMutability': 'view', 'type': 'function' },
        { 'constant': true, 'inputs': [], 'name': 'decimals', 'outputs': [{ 'name': '', 'type': 'uint8' }], 'payable': false, 'stateMutability': 'view', 'type': 'function' }
    ]";

    private Dictionary<string, uint> tokenDecimals;

    public decimal ConvertFromWei(string tokenAddress)
    {
        if (tokenDecimals.TryGetValue(tokenAddress, out var decimals)) {
            return decimals;
        }

        return 0;
    }

    public async Task<List<BigInteger>> GetTokenBalances(Web3 web3, string walletAddress, List<string> tokenAddresses)
    {
        var tokenBalances = new List<BigInteger>{};
        foreach (var tokenAddress in tokenAddresses)
        {
            var contract = web3.Eth.GetContract(abi, tokenAddress);
            var balanceFunction = contract.GetFunction("balanceOf");
            var decimalsFunction = contract.GetFunction("decimals");

            // Get the balance
            var balance = await balanceFunction.CallAsync<BigInteger>(walletAddress);
            var decimals = await decimalsFunction.CallAsync<uint>();
            tokenDecimals.Add(tokenAddress, decimals);
            tokenBalances.Add(balance);
        }

        return tokenBalances;
    }
}
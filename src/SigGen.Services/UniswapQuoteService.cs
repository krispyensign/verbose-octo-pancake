using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SigGen.Models.Uniswap;
using SigGen.Models;
using Nethereum.Web3;
using System.Numerics;
using System.ComponentModel;
using Nethereum.JsonRpc.Client;
using Nethereum.Contracts;
namespace SigGen.Services;

public class UniswapQuoteService : IQuoteService
{
    private readonly QuoteConfiguration _configuration;

    private readonly ILogger _logger;

    private readonly HttpClient _httpClient;
    private static JsonSerializerOptions serializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    private static JsonSerializerOptions serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private const string QuoteExactInputSingleFunctionABI = @"[{
        ""inputs"": [
            {
                ""name"": ""params"",
                ""type"": ""tuple"",
                ""components"": [
                    { ""name"": ""tokenIn"", ""type"": ""address"" },
                    { ""name"": ""tokenOut"", ""type"": ""address"" },
                    { ""name"": ""amountIn"", ""type"": ""uint256"" },
                    { ""name"": ""fee"", ""type"": ""uint24"" },
                    { ""name"": ""sqrtPriceLimitX96"", ""type"": ""uint160"" }
                ]
            }
        ],
        ""name"": ""quoteExactInputSingle"",
        ""outputs"": [
            { ""name"": ""amountOut"", ""type"": ""uint256"" },
            { ""name"": ""sqrtPriceX96After"", ""type"": ""uint160"" },
            { ""name"": ""initializedTicksCrossed"", ""type"": ""uint32"" },
            { ""name"": ""gasEstimate"", ""type"": ""uint256"" }
        ],
        ""payable"": false,
        ""type"": ""function""
    }]";
    private readonly Web3 web3;

    public UniswapQuoteService(QuoteConfiguration configuration, ILogger<UniswapQuoteService> logger)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _logger = logger 
            ?? throw new ArgumentNullException(nameof(logger));

        _httpClient = new()
        {
            BaseAddress = new Uri(_configuration.BaseAddress)
        };
        web3 = new Web3(_configuration.URL);

    }

    public static StringContent Construct(QuoteRequest request) 
    {
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request, serializeOptions),
            Encoding.UTF8,
            "application/json");
        jsonContent.Headers.Add("x-api-key", "JoyCGj29tT4pymvhaGciK4r1aIPvqW6W53xT1fwo");
        jsonContent.Headers.Add("x-request-source", "uniswap-web");
        jsonContent.Headers.Add("x-universal-router-version", "2.0");

        return jsonContent;
    }

    public async Task<string> GetQuote(
        string amount, string tokenIn, string tokenOut,
        CancellationToken cancellationToken = default)
    {
        _logger.BeginScope(nameof(UniswapQuoteService));
        _logger.LogInformation("using base Address {}", _configuration.BaseAddress);
        _logger.LogInformation("using path {}", _configuration.Path);
        var strat = new List<GasStrategy>
        {
            new()
        };
        var request = new QuoteRequest
        {
            Amount = amount,
            TokenIn = tokenIn,
            TokenOut = tokenOut,
            GasStrategies = strat,
        };

        var jsonContent = Construct(request);
        using HttpResponseMessage response =
            await _httpClient.PostAsync(_configuration.Path, jsonContent, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK) {
            _logger.LogError(response.ReasonPhrase);
            if (response.Content != null) {
                _logger.LogInformation(await response.Content.ReadAsStringAsync(cancellationToken));
            }
        }
        response.EnsureSuccessStatusCode();

        var quoteResponse = await response.Content.ReadFromJsonAsync<QuoteResponse>(serializeOptions, cancellationToken);
        if (quoteResponse?.Quote?.Output?.Amount == null)
        {
            var msg ="quote amount response was null.";
            try {
                _logger.LogInformation(quoteResponse?.ToString());
                _logger.LogError(msg);
            } catch {
                Console.WriteLine(msg);
            }

            throw new InvalidOperationException(msg);
        }

        _logger.LogInformation(await response.Content.ReadAsStringAsync(cancellationToken));
        return quoteResponse.Quote.Output.Amount;
    }

    public async Task<BigInteger> GetExactQuote(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta)
    {
        // Create the contract instance
        var contractAddress = _configuration.QuoterAddress;
        var contract = web3.Eth.GetContract(QuoteExactInputSingleFunctionABI, contractAddress);

        // Get the function handler
        var functionHandler = contract.GetFunction("quoteExactInputSingle");

        // Call the function
        var tokenIn = _configuration.Tokens[tokenSymbolIn];
        var tokenOut = _configuration.Tokens[tokenSymbolOut];
        var pool = _configuration.Pools.FirstOrDefault(p => p.Meta == meta && (
            p.Token0Name == tokenSymbolIn || p.Token0Name == tokenSymbolOut
        ) && (
            p.Token1Name == tokenSymbolIn || p.Token1Name == tokenSymbolOut
        )) ?? throw new InvalidOperationException("bad tokens");
        var fn = new QuoteExactInputSingleParams
        {
               TokenIn = tokenIn,
               TokenOut = tokenOut,
               Fee = pool.Fee,
               AmountIn = amountIn,
               SqrtPriceLimitX96 = 0,
        };

        try
        {
                var stuff = await functionHandler.CallAsync<(BigInteger, BigInteger, uint, BigInteger)>(fn);
        }
        catch (RpcResponseException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            // You can also inspect ex.Data for more details if available
        }

        return 0;
    }
}

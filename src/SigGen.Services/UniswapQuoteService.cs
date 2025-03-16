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
    };
    private static JsonSerializerOptions serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // temp ram cache
    public Dictionary<string, string>? InitValues { get; set; }
    public Dictionary<string, string>? SessionInitValues { get; set; }
    public string? CurrentToken { get; set; }

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
        var strat = new List<GasStrategy>
        {
            new()
        };
        var request = new QuoteRequest
        {
            Amount = amount,
            TokenIn = _configuration.Tokens[tokenIn],
            TokenOut = _configuration.Tokens[tokenOut],
            GasStrategies = strat,
            Swapper = _configuration.WalletAddress,
        };

        var jsonContent = Construct(request);
        using HttpResponseMessage response =
            await _httpClient.PostAsync(_configuration.Path, jsonContent, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK) {
            _logger.LogError(response.ReasonPhrase);
            if (response.Content != null) {
                _logger.LogInformation(await response.Content.ReadAsStringAsync(cancellationToken));
            }
            return "0";
        }

        // _logger.LogInformation(await response.Content.ReadAsStringAsync(cancellationToken));
        var quoteResponse = await response.Content.ReadFromJsonAsync<QuoteResponse>(serializeOptions, cancellationToken);
        return quoteResponse?.Quote?.Output?.Amount ?? "";
    }

     public async Task<Dictionary<string, string>> GetValueQuotes(Dictionary<string, BigInteger> balances, string startingToken)
    {
        if (string.IsNullOrWhiteSpace(startingToken))
        {
            _logger.LogError("CurrentToken must not be empty");
            return [];
        }
        var initBalances = new Dictionary<string, string>();
        var startingAmount = balances[startingToken];
        foreach(var t in _configuration.Tokens)
        {
            if (t.Key == startingToken)
            {
                initBalances.Add(startingToken, startingAmount.ToString());
                continue;
            }

            if (startingAmount == 0)
            {
                initBalances.Add(t.Key, "0");
                continue;
            }

            var result = await GetQuote(startingAmount.ToString(), startingToken, t.Key);
            initBalances.Add(t.Key, result.ToString());
        }

        return initBalances;
    }
}

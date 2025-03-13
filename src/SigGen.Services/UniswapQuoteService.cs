using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SigGen.Models;
namespace SigGen.Services;

public class UniswapQuoteService : IQuoteService
{
    private readonly QuoteConfiguration _configuration;

    private readonly ILogger _logger;

    private readonly HttpClient _httpClient;

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

        _logger.LogInformation("using base Address {}", _configuration.BaseAddress);
        _logger.LogInformation("using path {}", _configuration.Path);
    }

    public static StringContent Construct(QuoteRequest request) 
    {
        var serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
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

        var serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
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

        return quoteResponse.Quote.Output.Amount;
    }
}

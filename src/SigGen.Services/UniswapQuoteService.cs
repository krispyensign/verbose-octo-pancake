
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
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

    public async Task<string> GetQuote(
        string amount, string tokenIn, string tokenOut,
        CancellationToken cancellationToken = default)
    {
        _logger.BeginScope(nameof(UniswapQuoteService));
        var request = new QuoteRequest
        {
            Amount = amount,
            TokenIn = tokenIn,
            TokenOut = tokenOut,
            GasStrategies = new SortedDictionary<int, GasStrategy>
            {
                { 0, new GasStrategy() },
            },
        };

        using StringContent jsonContent = new(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");
        jsonContent.Headers.Add("x-api-key", "JoyCGj29tT4pymvhaGciK4r1aIPvqW6W53xT1fwo");
        jsonContent.Headers.Add("x-request-source", "uniswap-web");
        jsonContent.Headers.Add("x-universal-router-version", "2.0");
        using HttpResponseMessage response =
            await _httpClient.PostAsync(_configuration.Path, jsonContent, cancellationToken);

        if (response.StatusCode != HttpStatusCode.OK) {
            _logger.LogError(response.ReasonPhrase);
        }
        response.EnsureSuccessStatusCode();

        var quoteResponse = await response.Content.ReadFromJsonAsync<QuoteResponse>(cancellationToken);
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

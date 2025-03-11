
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SigGen.Models;
namespace SigGen.Services;

public class UniswapQuoteService : IQuoteService
{
    private QuoteConfiguration _configuration;

    private ILogger _logger;

    private HttpClient _httpClient;

    public UniswapQuoteService(IConfiguration configuration, ILogger logger)
    {
        _configuration = configuration?.GetValue<QuoteConfiguration>("Quote")
            ?? throw new ArgumentNullException(nameof(configuration));

        _logger = logger 
            ?? throw new ArgumentNullException(nameof(logger));

        _httpClient = new()
        {
            BaseAddress = new Uri(_configuration.BaseAddress)
        };
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
        using HttpResponseMessage response = await _httpClient.PostAsync(_configuration.Path, jsonContent, cancellationToken);
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

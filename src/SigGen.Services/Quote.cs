using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SigGen.Models;
namespace SigGen.Services;

public interface IQuoteService
{
    Task<string> GetQuote(
        string amount, string tokenIn, string tokenOut,
        CancellationToken cancellationToken = default);
}

public class QuoteService(
    string baseURL,
    ILogger logger) : IQuoteService
{
    private readonly string _baseURL = baseURL ??
        throw new ArgumentNullException(nameof(baseURL));
    private readonly ILogger _logger = logger ??
        throw new ArgumentNullException(nameof(logger));
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri(baseURL)
    };
    private const string quotePath = "";

    public async Task<string> GetQuote(
        string amount, string tokenIn, string tokenOut,
        CancellationToken cancellationToken = default)
    {
        _logger.BeginScope(nameof(QuoteService));
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

        var content = JsonContent.Create(request);
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(quotePath, content, cancellationToken);
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

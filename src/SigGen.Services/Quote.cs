using System.Net.Http.Json;
using SigGen.Models;

namespace SigGen.Services;

public interface IQuote
{
    Task<string> GetQuote(
        string amount, string tokenIn, string tokenOut,
        CancellationToken cancellationToken = default);
}

public class Quote(string baseURL) : IQuote
{
    private readonly string _baseURL = baseURL ??
        throw new ArgumentNullException(nameof(baseURL));
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri(baseURL)
    };
    private const string quotePath = "";

    public async Task<string> GetQuote(
        string amount, string tokenIn, string tokenOut,
        CancellationToken cancellationToken = default)
    {
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
            throw new InvalidOperationException("quote amount response was null");
        }

        return quoteResponse.Quote.Output.Amount;
    }
}

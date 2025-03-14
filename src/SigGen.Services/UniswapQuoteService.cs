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
using Nethereum.Uniswap.V4.V4Quoter;
using Nethereum.Uniswap.V4.V4Quoter.ContractDefinition;
using Nethereum.Uniswap.V4.PositionManager;
using Nethereum.Uniswap.V4;
using Nethereum.Uniswap.V4.StateView;
using Nethereum.Uniswap;
using Nethereum.Hex.HexConvertors.Extensions;

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

    public async Task<BigInteger> GetExactQuoteV4(BigInteger amountIn, string tokenSymbolIn, string tokenSymbolOut, string meta)
    {
        var pool = _configuration.Pools.FirstOrDefault(p => p.Meta == meta && (
            p.Token0Name == tokenSymbolIn || p.Token0Name == tokenSymbolOut
        ) && (
            p.Token1Name == tokenSymbolIn || p.Token1Name == tokenSymbolOut
        )) ?? throw new InvalidOperationException("bad tokens");
        var tokenIn = _configuration.Tokens[tokenSymbolIn];
        var tokenOut = _configuration.Tokens[tokenSymbolOut];
        var zero = "0x0000000000000000000000000000000000000000";
        var pk = new PoolKey
        {
            Currency0 = zero,
            Currency1 = tokenIn,
            Fee = pool.Fee,
            TickSpacing = pool.TickSpacing,
            Hooks = zero,
        };

        var v4Quoter = new V4QuoterService(web3, UniswapAddresses.BaseQuoterV4);

        var quoteExactParams = new QuoteExactSingleParams()
        {
            ExactAmount = amountIn,
            PoolKey = pk,
            ZeroForOne = false,
            HookData = zero.HexToByteArray(),
        };

        var quote = await v4Quoter.QuoteExactInputSingleQueryAsync(quoteExactParams);

        return quote.AmountOut;
    }
}

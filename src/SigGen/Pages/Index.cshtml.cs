using System.Numerics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;
using Nethereum.Util;
using SigGen.Models;
using SigGen.Services;

namespace siggen.Pages;

public class IndexModel(ILogger<IndexModel> logger, IQuoteService quoteService, IWalletService walletService, QuoteConfiguration configuration) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;
    private readonly IQuoteService _quoteService = quoteService;
    private readonly IWalletService _walletService = walletService;
    private static readonly decimal Tp = 1.001M;

    // Bind the posted property so that the input value is available in OnPost.
    [BindProperty]
    public string TokenInput { get; set; } = "ETH";
    public string CurrentToken { get; set; } = "ETH";

    // API
    public Dictionary<string, BigInteger> Balances { get; set;} = [];
    public Dictionary<string, string>? CurrentValues { get; set; }
    public Dictionary<string, string>? InitValues { get; set; }
    public Dictionary<string, string>? SessionInitValues { get; set; }

    public bool IsProfitable(
        string name,
        string currentValue,
        string sessionValue,
        string historicalValue
    )
    {
        if (!BigInteger.TryParse(currentValue, out BigInteger cv))
        {
            return false;
        }
        if (!BigDecimal.TryParse(sessionValue, out BigDecimal sv))
        {
            return false;
        }
        if (!BigDecimal.TryParse(historicalValue, out BigDecimal hv))
        {
            return false;
        }
        if (hv > cv)
        {
            return false;
        }
        BigDecimal svCalcTp = sv * Tp;
        if (cv > svCalcTp)
        {
            return true;
        }

        return false;
    }

    public string ProcessNameCache(string? currentToken)
    {
        if (!string.IsNullOrWhiteSpace(currentToken))
        {
            _quoteService.CurrentToken = currentToken;
        }

        return currentToken ?? "ETH";
    }

    public async Task<Dictionary<string, string>> ProcessValuesCache(string currentToken, bool resetSessionValues, Dictionary<string, string> cache)
    {
        if (resetSessionValues || cache.IsNullOrEmpty())
        {
            cache = CurrentValues.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        foreach (var b in Balances)
        {
            cache.TryAdd(b.Key, "0");
        }

        return cache;
    }

    public async Task<bool> UpdateValues(string? currentToken, bool resetSessionValues) 
    {
        currentToken = ProcessNameCache(currentToken);
        CurrentToken = currentToken;

        // get the wallet balances
        Balances = await _walletService.GetTokenBalances();

        // get the current values
        CurrentValues = await _quoteService.GetValueQuotes(Balances, currentToken);

        // populate the historical values if they exist
        _quoteService.InitValues = await ProcessValuesCache(currentToken, false, _quoteService.InitValues);
        InitValues = _quoteService.InitValues;

        // create a new session if the old one is no longer valid
        _quoteService.SessionInitValues = await ProcessValuesCache(currentToken, resetSessionValues, _quoteService.SessionInitValues);
        SessionInitValues = _quoteService.SessionInitValues;

        return true;
    }


    public async Task<IActionResult> OnGet() {
        await UpdateValues(null, false);

        return Page();
    }

    // When the form posts, this method will be executed.
    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        await UpdateValues(TokenInput, true);

        return Page();
    }

}

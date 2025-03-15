using System.Numerics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nethereum.Util;
using SigGen.Models;
using SigGen.Services;

namespace siggen.Pages;

public class IndexModel(ILogger<IndexModel> logger, IQuoteService quoteService, IWalletService walletService, QuoteConfiguration configuration) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;
    private readonly IQuoteService _quoteService = quoteService;
    private readonly IWalletService _walletService = walletService;
    private readonly QuoteConfiguration _configration = configuration;
    private static readonly string _sessionKeyName = "_TheKey";
    private static readonly decimal Tp = 1.001M;

    // Bind the posted property so that the input value is available in OnPost.
    [BindProperty]
    public string TokenInput { get; set; } = configuration.StartingToken;

    // API Call results
    public Dictionary<string, BigInteger> Balances { get; set;} = [];
    public Dictionary<string, string>? InitValues { get; set;}
    public Dictionary<string, string>? SessionInitValues { get; set; }
    public Dictionary<string, string>? CurrentValues { get; set; }
    public string? CurrentToken { get; set ; }

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

    public async Task<bool> UpdateValues() 
    {
        Balances = await _walletService.GetTokenBalances();

        InitValues ??= _configration.InitBalances;
        InitValues ??= await _quoteService.GetValueQuotes(Balances, TokenInput);

        CurrentValues = await _quoteService.GetValueQuotes(Balances, TokenInput);

        if (string.IsNullOrEmpty(HttpContext.Session.GetString(_sessionKeyName)))
        {
            HttpContext.Session.SetString(_sessionKeyName, "x");
            foreach(var kv in CurrentValues)
            {
                HttpContext.Session.SetString(kv.Key, kv.Value);
            }
            SessionInitValues = await _quoteService.GetValueQuotes(Balances, TokenInput);
        }
        else
        {
            SessionInitValues = [];
            foreach(var kv in CurrentValues)
            {
                SessionInitValues.Add(kv.Key, HttpContext.Session.GetString(kv.Key) ?? "");
            }
        }
        SessionInitValues ??= CurrentValues;

        return true;
    }


    public async Task<IActionResult> OnGet() {
        await UpdateValues();

        return Page();
    }

    // When the form posts, this method will be executed.
    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        HttpContext.Session.Clear();
        await UpdateValues();

        return Page();
    }

}

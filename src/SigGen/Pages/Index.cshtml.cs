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
    private readonly QuoteConfiguration _configration = configuration;
    private static readonly string _sessionKeyName = "_TheKey";
    private static readonly string _sessionCurrentToken = "_CurrentToken";
    private static readonly decimal Tp = 1.001M;

    // Bind the posted property so that the input value is available in OnPost.
    [BindProperty]
    public string TokenInput { get; set; } = "";

    // API
    public string CurrentToken { get; set ; } = "";
    public Dictionary<string, BigInteger> Balances { get; set;} = [];
    public Dictionary<string, string>? InitValues { get; set;}
    public Dictionary<string, string>? SessionInitValues { get; set; }
    public Dictionary<string, string>? CurrentValues { get; set; }

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
        // check if this is a new session
        var isNewSession = string.IsNullOrEmpty(HttpContext.Session.GetString(_sessionKeyName));
        if (!isNewSession)
        {
            CurrentToken = HttpContext.Session.GetString(_sessionCurrentToken) ?? "";   
        }

        if (string.IsNullOrWhiteSpace(CurrentToken))
        {
            _logger.LogError("CurrentToken must not be empty");
        }

        // get the wallet balances
        Balances = await _walletService.GetTokenBalances();

        // get the current values
        CurrentValues = await _quoteService.GetValueQuotes(Balances, CurrentToken);

        // populate the historical values if they exist
        InitValues ??= _configration.InitBalances;
        InitValues ??= CurrentValues.ToDictionary(kv => kv.Key, kv => kv.Value);
        foreach (var b in Balances)
        {
            if (!InitValues.ContainsKey(b.Key))
            {
                InitValues.Add(b.Key, "0");
            }
        }

        // create a new session if the old one is no longer valid
        if (isNewSession)
        {
            foreach(var kv in CurrentValues)
            {
                HttpContext.Session.SetString(kv.Key, kv.Value);
            }
            SessionInitValues = await _quoteService.GetValueQuotes(Balances, CurrentToken);
        }
        else
        {
            SessionInitValues = [];
            foreach(var kv in CurrentValues)
            {
                SessionInitValues.Add(kv.Key, HttpContext.Session.GetString(kv.Key) ?? "");
            }
        }
        SessionInitValues ??= CurrentValues.ToDictionary(kv => kv.Key, kv => kv.Value);

        HttpContext.Session.SetString(_sessionKeyName, "x");

        return true;
    }


    public async Task<IActionResult> OnGet() {
        if (!string.IsNullOrWhiteSpace(CurrentToken))
        {
            await UpdateValues();
        }

        return Page();
    }

    // When the form posts, this method will be executed.
    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        CurrentToken = TokenInput;
        HttpContext.Session.Clear();
        await UpdateValues();

        return Page();
    }

}

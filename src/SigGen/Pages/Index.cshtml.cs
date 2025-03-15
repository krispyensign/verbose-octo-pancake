using System.Numerics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SigGen.Models;
using SigGen.Services;

namespace siggen.Pages;

public class IndexModel(ILogger<IndexModel> logger, IQuoteService quoteService, IWalletService walletService, QuoteConfiguration configuration) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;
    private readonly IQuoteService _quoteService = quoteService;
    private readonly IWalletService _walletService = walletService;
    private readonly QuoteConfiguration _configration = configuration;

    // Bind the posted property so that the input value is available in OnPost.
    [BindProperty]
    public string? TokenInput { get; set; }
    [BindProperty]
    public string? TokenOutput { get; set; }
    [BindProperty]
    public string? Amount { get; set; }

    // API Call results
    public Dictionary<string, BigInteger> Balances { get; set;} = [];
    public Dictionary<string, BigInteger> InitBalances { get; set;} = [];
    public Dictionary<string, BigInteger> CurrentBalances { get; set; } = [];
    public string? QuoteResult1 { get; set; }
    public string? QuoteResult2 { get; set; }
    public string? CurrentToken { get; set ; }

    public async Task<IActionResult> OnGet() {
        Balances = await _walletService.GetTokenBalances();
        InitBalances = await _quoteService.GetValueQuotes(Balances, _configration.StartingToken, true);
        CurrentToken ??= _configration.StartingToken;

        CurrentBalances = await _quoteService.GetValueQuotes(Balances, CurrentToken, false);

        return Page();
    }

    // When the form posts, this method will be executed.
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (TokenInput == null || TokenOutput == null)
        {
            _logger.LogError("TokenInput and TokenOutput cannot be null");

            return Page();
        }

        // Call the API service using the input from the textbox.
        if (BigInteger.TryParse(Amount, out var val))
        {
            var quoteResult1 = await _quoteService.GetExactQuoteV2(val, TokenInput, TokenOutput, "");
            QuoteResult1 = quoteResult1.ToString();
            var quoteResult2 = await _quoteService.GetExactQuoteV4(val, TokenInput, TokenOutput, "");
            QuoteResult2 = quoteResult2.ToString();
        }
        else
        {
            _logger.LogError("Amount must be of type BigInt");
            return  Page();
        }

        Balances = await _walletService.GetTokenBalances();

        // Optionally, you might want to return a RedirectToPage or simply refresh the page.
        return Page();
    }

}

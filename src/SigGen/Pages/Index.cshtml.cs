using System.Numerics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SigGen.Services;

namespace siggen.Pages;

public class IndexModel(ILogger<IndexModel> logger, IQuoteService quoteService, IWalletService walletService) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;
    private readonly IQuoteService _quoteService = quoteService;
    private readonly IWalletService _walletService = walletService;


    // Bind the posted property so that the input value is available in OnPost.
    [BindProperty]
    public string TokenInput { get; set; }
    [BindProperty]
    public string TokenOutput { get; set; }
    [BindProperty]
    public string Amount { get; set; }

    // API Call results
    public Dictionary<string, BigInteger> Balances;
    public string QuoteResult1 { get; set; }
    public string QuoteResult2 { get; set; }

    public async Task<IActionResult> OnGet() {
        Balances = await _walletService.GetTokenBalances();

        return Page();
    }

    // When the form posts, this method will be executed.
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Call the API service using the input from the textbox.
        if (UInt128.TryParse(Amount, out var val))
        {
            var quoteResult1 = await _quoteService.GetExactQuoteV2(val, TokenInput, TokenOutput, "");
            QuoteResult1 = quoteResult1.ToString();
            var quoteResult2 = await _quoteService.GetExactQuoteV4(val, TokenInput, TokenOutput, "");
            QuoteResult2 = quoteResult2.ToString();
        }

        Balances = await _walletService.GetTokenBalances();

        // Optionally, you might want to return a RedirectToPage or simply refresh the page.
        return Page();
    }

}

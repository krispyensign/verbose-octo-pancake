using System.Numerics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

    // Bind the posted property so that the input value is available in OnPost.
    [BindProperty]
    public string? TokenIn0 { get; set; }
    [BindProperty]
    public string? TokenIn1 { get; set; }
    [BindProperty]
    public string? Amount { get; set; }

    // API Call results
    public Dictionary<string, List<BigInteger>>? Results;

    public void OnGet() 
    {
    }

    // When the form posts, this method will be executed.
    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Results = await _quoteService.GetValueQuotes(TokenIn0, TokenIn1);

        return Page();
    }

}

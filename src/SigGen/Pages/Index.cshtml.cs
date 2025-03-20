using System.Numerics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nethereum.Util;
using SigGen.Models;
using SigGen.Services;

namespace siggen.Pages;

public class IndexModel(ILogger<IndexModel> logger, IQuoteService quoteService) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;
    private readonly IQuoteService _quoteService = quoteService;

    // Bind the posted property so that the input value is available in OnPost.
    [BindProperty]
    public string? TokenIn0 { get; set; }
    [BindProperty]
    public string? TokenIn1 { get; set; }
    [BindProperty]
    public string? Amount { get; set; }

    // API Call results
    public List<(string, BigDecimal)>? Results;
    public BigDecimal? GasPrice;

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
        GasPrice = await _quoteService.GetGasPrice();
        

        var sellFirst = 1 * Results[0].Item2;
        var unwrapped = sellFirst - GasPrice;
        var sellSecond = unwrapped * Results[4].Item2;
        var hope = sellSecond;

        return Page();
    }

}

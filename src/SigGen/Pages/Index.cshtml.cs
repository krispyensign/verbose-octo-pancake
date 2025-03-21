using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nethereum.Contracts.QueryHandlers.MultiCall;
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
    public string? Amount0 { get; set; }
    [BindProperty]
    public string? Amount1 { get; set; }

    // API Call results
    public List<(string, BigDecimal)>? Results;
    public BigDecimal? GasPrice;
    public List<BigDecimal> ArbSellResults;

    private static readonly decimal unit = 1000000000000000000M;
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

        if (!BigDecimal.TryParse(Amount0, out var amt0))
        {
            _logger.LogError("failed to parse amount0");
            return Page();
        }
        var iamt0 = (amt0 * unit).FloorToBigInteger();

        if (!BigDecimal.TryParse(Amount1, out var amt1))
        {
            _logger.LogError("failed to parse amount1");
            return Page();
        }
        var iamt1 = (amt1 * unit).FloorToBigInteger();

        GasPrice = await _quoteService.GetGasPrice();
        if (GasPrice == null)
        {
            _logger.LogError("failed to retrieve gas prices");
            return Page();
        }
        var workingGas = (BigInteger)GasPrice;
        GasPrice /= unit;
        ArbSellResults = [];

        var result1 = await NewMethod1(iamt0, workingGas);
        ArbSellResults.Add((BigDecimal)result1 / unit);

        var result2 = await NewMethod2(iamt0, workingGas);
        ArbSellResults.Add((BigDecimal)result2 / unit);

        var result0 = await NewMethod(iamt1, workingGas);
        ArbSellResults.Add((BigDecimal)result0 / unit);

        var result3 = await NewMethod3(iamt1, workingGas);
        ArbSellResults.Add((BigDecimal)result3 / unit);

        return Page();
    }

    private async Task<BigInteger> NewMethod1(BigInteger iamt0, BigInteger workingGas)
    {
        // DRB => WETH => ETH => DRB
        var sellFirst = await _quoteService.GetExactQuoteV2(iamt0, TokenIn0, TokenIn1, "v3");
        var unwrapped = sellFirst - workingGas;
        var sellSecond = await _quoteService.GetExactQuoteV4(unwrapped, TokenIn1, TokenIn0, "v4");
        var result = sellSecond - iamt0;
        return result;
    }

    private async Task<BigInteger> NewMethod2(BigInteger iamt0, BigInteger workingGas)
    {
        // DRB => ETH => WETH => DRB
        var sellFirst = await _quoteService.GetExactQuoteV4(iamt0, TokenIn0, TokenIn1, "v4");
        var wrapped = sellFirst - workingGas;
        var sellSecond = await _quoteService.GetExactQuoteV2(wrapped, TokenIn1, TokenIn0, "v3");
        var result = sellSecond - iamt0;
        return result;
    }

    private async Task<BigInteger> NewMethod(BigInteger iamt1, BigInteger workingGas)
    {
        // ETH => WETH => DRB => ETH
        var wrapped = iamt1 - workingGas;
        var sellFirst = await _quoteService.GetExactQuoteV2(wrapped, TokenIn1, TokenIn0, "v3");
        var sellSecond = await _quoteService.GetExactQuoteV4(sellFirst, TokenIn0, TokenIn1, "v4");
        var result = sellSecond - iamt1;
        return result;
    }
    
    private async Task<BigInteger> NewMethod3(BigInteger iamt1, BigInteger workingGas)
    {
        // // ETH => DRB => WETH => ETH
        var sellFirst = await _quoteService.GetExactQuoteV4(iamt1, TokenIn1, TokenIn0, "v4");
        var sellSecond = await _quoteService.GetExactQuoteV2(sellFirst, TokenIn0, TokenIn1, "v3") - workingGas;
        var result = sellSecond - iamt1;
        return result;
    }
}

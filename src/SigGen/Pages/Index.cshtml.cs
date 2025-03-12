using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SigGen.Services;

namespace siggen.Pages;

public class IndexModel(ILogger<IndexModel> logger, IQuoteService quoteService) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;
    private readonly IQuoteService _quoteService = quoteService;

    // Bind the posted property so that the input value is available in OnPost.
    [BindProperty]
    public string TokenInput { get; set; }
    [BindProperty]
    public string TokenOutput { get; set; }
    [BindProperty]
    public string Amount { get; set; }

    // This property holds the result of the API call.
    public string QuoteResult { get; set; }

    public void OnGet() {}

    // When the form posts, this method will be executed.
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Call the API service using the input from the textbox.
        QuoteResult = await _quoteService.GetQuote(Amount, TokenInput, TokenOutput);

        // Optionally, you might want to return a RedirectToPage or simply refresh the page.
        return Page();
    }
}

using SigGen.Models;
using SigGen.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLogging(builder => builder.AddConsole());
builder.Services.AddSingleton<QuoteConfiguration>(
    _ => builder.Configuration.GetSection("Quote").Get<QuoteConfiguration>()
);
builder.Services.AddSingleton<IWalletService, WalletService>();
builder.Services.AddSingleton<IQuoteService, UniswapQuoteService>();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

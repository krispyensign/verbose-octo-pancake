using Microsoft.Extensions.DependencyInjection;
using SigGen.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLogging(builder => builder.AddConsole());
builder.Services.AddSingleton<IQuoteService>(s => {
    var logger = s.GetRequiredService<ILogger<IQuoteService>>();
    return new QuoteService(
        "https://www.example.com", 
        logger);
});
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

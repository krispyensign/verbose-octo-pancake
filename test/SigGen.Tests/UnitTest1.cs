namespace SigGen.Tests;

using SigGen.Services;
using System;
using Xunit;
using SigGen.Models.Uniswap;
using Newtonsoft.Json;

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        var request = new QuoteRequest
        {
            Amount = "1000000000000000000",
            TokenIn = "0x0000000000000000000000000000000000000000",
            TokenOut = "0xcbb7c0000ab88b473b1f5afd9ef808440eed33bf",
            GasStrategies = new List<GasStrategy>
            {
                new(),
            },
        };
        var j = UniswapQuoteService.Construct(request);
        var r = await j.ReadAsStringAsync();
        Assert.NotEmpty(r);
    }

    [Fact]
    public void TestLoadRequestJson()
    {
        string? json;
        using (var r = new StreamReader("quoteRequest.json"))
        {
            json = r.ReadToEnd();
        }
        var quoteRequest = JsonConvert.DeserializeObject<QuoteRequest>(json);
        Assert.NotNull(quoteRequest);
        Assert.Equal("1000000000000000000", quoteRequest.Amount);
    }
    
    [Fact]
    public void TestLoadResponseJson()
    {
        string? json;
        using (var r = new StreamReader("quoteResponse.json"))
        {
            json = r.ReadToEnd();
        }
        var quoteResponse = JsonConvert.DeserializeObject<QuoteResponse>(json);
        Assert.NotNull(quoteResponse?.Quote?.Input?.Amount);
        Assert.Equal("1000000000000000000", quoteResponse.Quote.Input.Amount);
    }
}

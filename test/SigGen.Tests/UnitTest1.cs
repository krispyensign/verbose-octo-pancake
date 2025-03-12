namespace SigGen.Tests;

using SigGen.Services;
using System;
using Xunit;
using SigGen.Models;

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
        Console.WriteLine(r);

    }

}

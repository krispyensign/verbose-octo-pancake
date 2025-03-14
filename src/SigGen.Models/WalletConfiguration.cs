using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;

namespace SigGen.Models;

public class WalletConfiguration
{
    public string URL;
    public string WalletAddress;
    public string QuoterAddress;
    public List<string> Tokens;
    public List<string> Pools;
}
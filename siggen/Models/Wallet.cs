using System.ComponentModel.DataAnnotations;

namespace SigGen.Models;

public class Wallet(string id, string name, string description)
{
    public string Id { get; set; } = id;
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;
}
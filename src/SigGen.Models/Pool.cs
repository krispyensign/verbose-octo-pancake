namespace SigGen.Models;

public class Pool {
    public string Name { get; set; }
    public string Meta { get; set; }
    public uint Fee { get; set; }
    public string Token0Name { get; set; }
    public string Token1Name { get; set; }
    public int TickSpacing { get; set; }
}
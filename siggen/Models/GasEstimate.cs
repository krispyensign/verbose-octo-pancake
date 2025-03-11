namespace SigGen.Models;

public class GasEstimate {
    public string Type { get; set; }
    public GasStrategy Strategy { get; set; }
    public string GasLimit { get; set; }
    public string GasFee { get; set; }
    public string GasPrice { get; set; }
}
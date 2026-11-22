namespace PredictiveMaintenance.Models;

public class Threshold
{
    public int UserId { get; set; }
    public int AssetId { get; set; }

    public double RmsMin { get; set; }
    public double RmsMax { get; set; }
    public double TempMin { get; set; }
    public double TempMax { get; set; }

    public Asset? Asset { get; set; }
}

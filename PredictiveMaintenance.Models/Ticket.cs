namespace PredictiveMaintenance.Models;

public class Ticket
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AssetId { get; set; }

    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "Open";

    public Asset? Asset { get; set; }
}

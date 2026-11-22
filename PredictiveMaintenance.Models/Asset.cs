namespace PredictiveMaintenance.Models;

public class Asset
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public User? User { get; set; }
    public ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
    public Threshold? Threshold { get; set; }
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

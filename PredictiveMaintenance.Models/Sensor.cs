namespace PredictiveMaintenance.Models;

public class Sensor
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string SensorName { get; set; } = string.Empty;

    public Asset? Asset { get; set; }
    public ICollection<Reading> Readings { get; set; } = new List<Reading>();
}

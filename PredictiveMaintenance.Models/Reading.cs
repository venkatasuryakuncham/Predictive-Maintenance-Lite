namespace PredictiveMaintenance.Models;

public class Reading
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SensorId { get; set; }

    public double Rms { get; set; }
    public double Temperature { get; set; }
    public DateTime Timestamp { get; set; }

    public Sensor? Sensor { get; set; }
}

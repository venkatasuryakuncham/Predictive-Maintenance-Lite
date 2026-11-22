using PredictiveMaintenance.Models;

namespace PredictiveMaintenance.Simulator;

public class AggregationService
{
    public ReadingSummary GetSummary(List<Reading> readings)
    {
        if (readings == null || readings.Count == 0)
        {
            return new ReadingSummary
            {
                Count = 0,
                MinRms = 0, MaxRms = 0, AvgRms = 0,
                MinTemp = 0, MaxTemp = 0, AvgTemp = 0
            };
        }

        return new ReadingSummary
        {
            Count   = readings.Count,
            MinRms  = readings.Min(r => r.Rms),
            MaxRms  = readings.Max(r => r.Rms),
            AvgRms  = readings.Average(r => r.Rms),
            MinTemp = readings.Min(r => r.Temperature),
            MaxTemp = readings.Max(r => r.Temperature),
            AvgTemp = readings.Average(r => r.Temperature)
        };
    }

    public Dictionary<int, ReadingSummary> GetSummaryBySensor(List<Reading> readings)
    {
        return readings
            .GroupBy(r => r.SensorId)
            .ToDictionary(g => g.Key, g => GetSummary(g.ToList()));
    }
}

public class ReadingSummary
{
    public int Count { get; set; }
    public double MinRms { get; set; }
    public double MaxRms { get; set; }
    public double AvgRms { get; set; }
    public double MinTemp { get; set; }
    public double MaxTemp { get; set; }
    public double AvgTemp { get; set; }

    public override string ToString()
    {
        return $"n={Count} | RMS  min={MinRms:F2} max={MaxRms:F2} avg={AvgRms:F2} " +
               $"| Temp min={MinTemp:F2} max={MaxTemp:F2} avg={AvgTemp:F2}";
    }
}

using System.Net.Http.Json;
using PredictiveMaintenance.Models;
using PredictiveMaintenance.Simulator;

const string ApiBaseUrl  = "http://localhost:5001";
const int    LoopDelayMs = 3000;

var sensors = new Dictionary<(int UserId, int Id), string>();

using var http = new HttpClient
{
    BaseAddress = new Uri(ApiBaseUrl),
    Timeout     = TimeSpan.FromSeconds(10)
};

var rng            = new Random();
var aggregator     = new AggregationService();
var recentReadings = new List<Reading>(capacity: 200);

PrintBanner();

await WaitForApiAsync(http);
await RefreshSensorsAsync(http, sensors);

Console.WriteLine();
if (sensors.Count == 0)
{
    Console.WriteLine("No sensors yet — waiting for a user to add an asset...");
}
else
{
    Console.WriteLine($"Simulating: {string.Join(", ", sensors.Values)}");
}
Console.WriteLine("Starting reading loop (Ctrl+C to stop)...");
Console.WriteLine(new string('-', 70));

int tick = 0;

while (true)
{
    await RefreshSensorsAsync(http, sensors);

    foreach (var (key, name) in sensors.ToList())
    {
        var reading = BuildRandomReading(key.UserId, key.Id, rng);

        try
        {
            var response = await http.PostAsJsonAsync("/api/readings", reading);

            if (response.IsSuccessStatusCode)
            {
                recentReadings.Add(reading);
                if (recentReadings.Count > 100) recentReadings.RemoveAt(0);

                string spike = reading.Rms > 3.0 || reading.Temperature > 80
                    ? "  <<< SPIKE"
                    : "";

                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] Sent: {name} " +
                    $"| RMS: {reading.Rms,5:F2} | Temp: {reading.Temperature,6:F2}°C{spike}");
            }
        }
        catch
        {
        }
    }

    tick++;

    if (tick % 10 == 0 && recentReadings.Count > 0)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"---- Summary (last {recentReadings.Count} readings) ----");
        Console.WriteLine(aggregator.GetSummary(recentReadings));
        Console.WriteLine("-------------------------------------------------------");
        Console.ResetColor();
        Console.WriteLine();
    }

    Thread.Sleep(LoopDelayMs);
}

static void PrintBanner()
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("=======================================================");
    Console.WriteLine("   PredictiveMaintenance.Simulator                     ");
    Console.WriteLine("   Synthetic IoT sensor reading generator              ");
    Console.WriteLine("=======================================================");
    Console.ResetColor();
}

static async Task WaitForApiAsync(HttpClient http)
{
    Console.Write("Waiting for API at " + http.BaseAddress + " ");
    for (int attempt = 1; attempt <= 20; attempt++)
    {
        try
        {
            var resp = await http.GetAsync("/api/sensors");
            if (resp.IsSuccessStatusCode)
            {
                Console.WriteLine(" OK");
                return;
            }
        }
        catch { }

        Console.Write(".");
        await Task.Delay(1000);
    }

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ERROR: API did not respond after 20 seconds. " +
                      "Start PredictiveMaintenance.API first.");
    Console.ResetColor();
    Environment.Exit(1);
}

static async Task RefreshSensorsAsync(
    HttpClient http,
    Dictionary<(int UserId, int Id), string> sensors)
{
    try
    {
        var remote = await http.GetFromJsonAsync<List<Sensor>>("/api/sensors");
        if (remote == null) return;

        var remoteKeys = new HashSet<(int, int)>();
        foreach (var s in remote)
        {
            var key = (s.UserId, s.Id);
            remoteKeys.Add(key);
            if (!sensors.ContainsKey(key))
            {
                sensors[key] = s.SensorName;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] + Added sensor \"{s.SensorName}\"");
                Console.ResetColor();
            }
        }

        var goneKeys = sensors.Keys.Where(k => !remoteKeys.Contains(k)).ToList();
        foreach (var key in goneKeys)
        {
            var name = sensors[key];
            sensors.Remove(key);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] - Removed sensor \"{name}\" (no longer active)");
            Console.ResetColor();
        }
    }
    catch
    {
    }
}

static Reading BuildRandomReading(int userId, int sensorId, Random rng)
{
    bool tempSpike = rng.Next(5) == 0;
    bool rmsSpike  = rng.Next(8) == 0;

    double temp = tempSpike
        ? 80.0 + rng.NextDouble() * 20.0
        : 50.0 + rng.NextDouble() * 25.0;

    double rms = rmsSpike
        ? 3.5 + rng.NextDouble() * 1.5
        : 0.5 + rng.NextDouble() * 2.0;

    return new Reading
    {
        UserId      = userId,
        SensorId    = sensorId,
        Rms         = Math.Round(rms,  2),
        Temperature = Math.Round(temp, 2),
        Timestamp   = IstClock.Now
    };
}

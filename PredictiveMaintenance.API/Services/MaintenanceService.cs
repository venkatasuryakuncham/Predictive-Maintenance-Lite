using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.Data;
using PredictiveMaintenance.Models;

namespace PredictiveMaintenance.API.Services;

public class MaintenanceService
{
    private readonly AppDbContext _db;

    public MaintenanceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task EvaluateThresholdsAsync(Reading reading)
    {
        var threshold = await _db.Thresholds.FirstOrDefaultAsync(
            t => t.UserId == reading.UserId && t.AssetId == reading.SensorId);
        if (threshold == null) return;

        var reasons = new List<string>();

        if (reading.Rms > threshold.RmsMax)
            reasons.Add($"RMS exceeded (spike high): {reading.Rms:F2} > max {threshold.RmsMax:F2}");
        if (reading.Rms < threshold.RmsMin)
            reasons.Add($"RMS below minimum (spike low): {reading.Rms:F2} < min {threshold.RmsMin:F2}");
        if (reading.Temperature > threshold.TempMax)
            reasons.Add($"Temperature exceeded (spike high): {reading.Temperature:F2}°C > max {threshold.TempMax:F2}°C");
        if (reading.Temperature < threshold.TempMin)
            reasons.Add($"Temperature below minimum (spike low): {reading.Temperature:F2}°C < min {threshold.TempMin:F2}°C");

        if (reasons.Count == 0) return;

        foreach (var reason in reasons)
        {
            bool duplicateExists = await _db.Tickets.AnyAsync(t =>
                t.UserId == reading.UserId &&
                t.AssetId == reading.SensorId &&
                t.Status == "Open" &&
                t.Reason == reason);

            if (duplicateExists) continue;

            _db.Tickets.Add(new Ticket
            {
                UserId = reading.UserId,
                AssetId = reading.SensorId,
                Reason = reason,
                CreatedAt = IstClock.Now,
                Status = "Open"
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<Asset>> GetAssetsWithOpenTicketsAsync(int userId)
    {
        var ownedAssetIds = await _db.Tickets
            .Where(t => t.UserId == userId && t.Status == "Open")
            .Select(t => t.AssetId)
            .Distinct()
            .ToListAsync();

        return await _db.Assets
            .Where(a => a.UserId == userId && ownedAssetIds.Contains(a.Id))
            .Include(a => a.Sensors)
            .Include(a => a.Threshold)
            .ToListAsync();
    }
}

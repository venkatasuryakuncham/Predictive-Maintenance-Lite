using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.Data;
using PredictiveMaintenance.Models;

namespace PredictiveMaintenance.API.Services;

public class QueryService
{
    private readonly AppDbContext _db;

    public QueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Asset>> GetAssetsWithViolationsLast24HoursAsync()
    {
        var cutoff = IstClock.Now.AddHours(-24);

        var violatingKeys = await (
            from reading   in _db.Readings
            join threshold in _db.Thresholds
                on new { reading.UserId, AssetId = reading.SensorId }
                equals new { threshold.UserId, threshold.AssetId }
            where reading.Timestamp >= cutoff
               && (reading.Rms > threshold.RmsMax || reading.Temperature > threshold.TempMax)
            select new { reading.UserId, AssetId = reading.SensorId }
        ).Distinct().ToListAsync();

        var userIds = violatingKeys.Select(k => k.UserId).Distinct().ToList();
        var assetIds = violatingKeys.Select(k => k.AssetId).Distinct().ToList();

        var candidates = await _db.Assets
            .Where(a => userIds.Contains(a.UserId) && assetIds.Contains(a.Id))
            .Include(a => a.Sensors)
                .ThenInclude(s => s.Readings.Where(r => r.Timestamp >= cutoff))
            .Include(a => a.Threshold)
            .ToListAsync();

        var keySet = violatingKeys.Select(k => (k.UserId, k.AssetId)).ToHashSet();
        return candidates.Where(a => keySet.Contains((a.UserId, a.Id))).ToList();
    }

    public async Task<List<object>> GetAverageRmsPerAssetPerDayAsync()
    {
        var rows = await (
            from reading in _db.Readings
            join asset   in _db.Assets
                on new { reading.UserId, AssetId = reading.SensorId }
                equals new { asset.UserId, AssetId = asset.Id }
            group reading by new
            {
                reading.UserId,
                AssetId = reading.SensorId,
                AssetName = asset.Name,
                Date = reading.Timestamp.Date
            }
            into g
            select new
            {
                UserId     = g.Key.UserId,
                AssetId    = g.Key.AssetId,
                AssetName  = g.Key.AssetName,
                Date       = g.Key.Date,
                AverageRms = g.Average(r => r.Rms)
            }
        ).ToListAsync();

        return rows.Cast<object>().ToList();
    }

    public async Task<int> GetOpenTicketCountAsync()
    {
        return await _db.Tickets.CountAsync(t => t.Status == "Open");
    }
}

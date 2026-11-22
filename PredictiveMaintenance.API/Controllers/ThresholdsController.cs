using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.API.Services;
using PredictiveMaintenance.Data;
using PredictiveMaintenance.Models;

namespace PredictiveMaintenance.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ThresholdsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CurrentUserAccessor _currentUser;

    public ThresholdsController(AppDbContext db, CurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<Threshold>>> GetAll()
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        return await _db.Thresholds
            .Include(t => t.Asset)
            .Where(t => t.UserId == userId)
            .ToListAsync();
    }

    [HttpGet("{assetId}")]
    public async Task<ActionResult<Threshold>> GetByAsset(int assetId)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        var threshold = await _db.Thresholds
            .Include(t => t.Asset)
            .FirstOrDefaultAsync(t => t.AssetId == assetId && t.UserId == userId);

        if (threshold == null)
            return NotFound($"No threshold found for asset {assetId}.");

        return threshold;
    }

    [HttpPost]
    public async Task<ActionResult<Threshold>> Create(Threshold threshold)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        bool owns = await _db.Assets.AnyAsync(a => a.Id == threshold.AssetId && a.UserId == userId);
        if (!owns) return BadRequest($"Asset {threshold.AssetId} does not exist.");

        bool alreadyHas = await _db.Thresholds
            .AnyAsync(t => t.UserId == userId && t.AssetId == threshold.AssetId);
        if (alreadyHas)
            return Conflict($"Asset {threshold.AssetId} already has a threshold. Use PUT to update it.");

        threshold.UserId = userId.Value;
        _db.Thresholds.Add(threshold);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByAsset), new { assetId = threshold.AssetId }, threshold);
    }

    [HttpPut("{assetId}")]
    public async Task<IActionResult> Update(int assetId, Threshold updated)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        var threshold = await _db.Thresholds
            .FirstOrDefaultAsync(t => t.AssetId == assetId && t.UserId == userId);
        if (threshold == null) return NotFound($"No threshold found for asset {assetId}.");

        threshold.RmsMin = updated.RmsMin;
        threshold.RmsMax = updated.RmsMax;
        threshold.TempMin = updated.TempMin;
        threshold.TempMax = updated.TempMax;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{assetId}")]
    public async Task<IActionResult> Delete(int assetId)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        var threshold = await _db.Thresholds
            .FirstOrDefaultAsync(t => t.AssetId == assetId && t.UserId == userId);
        if (threshold == null) return NotFound($"No threshold found for asset {assetId}.");

        _db.Thresholds.Remove(threshold);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

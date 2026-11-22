using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.API.Services;
using PredictiveMaintenance.Data;
using PredictiveMaintenance.Models;

namespace PredictiveMaintenance.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CurrentUserAccessor _currentUser;

    public AssetsController(AppDbContext db, CurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<Asset>>> GetAll()
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        return await _db.Assets
            .Where(a => a.UserId == userId)
            .Include(a => a.Sensors)
            .Include(a => a.Threshold)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Asset>> GetById(int id)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        var asset = await _db.Assets
            .Include(a => a.Sensors)
            .Include(a => a.Threshold)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (asset == null)
            return NotFound($"Asset {id} not found.");

        return asset;
    }

    public record CreateAssetRequest(
        int Id,
        string Name,
        string? Location,
        string? Description,
        string SensorName);

    [HttpPost]
    public async Task<ActionResult<Asset>> Create([FromBody] CreateAssetRequest req)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        if (req.Id <= 0)
            return BadRequest("Asset ID must be a positive number.");
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Asset name is required.");
        if (string.IsNullOrWhiteSpace(req.SensorName))
            return BadRequest("Sensor name is required.");

        var duplicate = await _db.Assets
            .AnyAsync(a => a.UserId == userId && a.Id == req.Id);
        if (duplicate)
            return BadRequest($"Asset ID {req.Id} already exists.");

        var asset = new Asset
        {
            Id = req.Id,
            UserId = userId.Value,
            Name = req.Name.Trim(),
            Location = (req.Location ?? "").Trim(),
            Description = (req.Description ?? "").Trim(),
        };
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync();

        _db.Sensors.Add(new Sensor
        {
            Id = req.Id,
            UserId = userId.Value,
            SensorName = req.SensorName.Trim(),
        });

        _db.Thresholds.Add(new Threshold
        {
            UserId = userId.Value,
            AssetId = req.Id,
            RmsMin = 0.5,
            RmsMax = 3.0,
            TempMin = 40.0,
            TempMax = 80.0
        });

        await _db.SaveChangesAsync();

        var created = await _db.Assets
            .Include(a => a.Sensors)
            .Include(a => a.Threshold)
            .FirstAsync(a => a.UserId == userId && a.Id == req.Id);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Asset updated)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (asset == null) return NotFound($"Asset {id} not found.");

        asset.Name = updated.Name;
        asset.Location = updated.Location;
        asset.Description = updated.Description;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (asset == null) return NotFound($"Asset {id} not found.");

        _db.Assets.Remove(asset);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

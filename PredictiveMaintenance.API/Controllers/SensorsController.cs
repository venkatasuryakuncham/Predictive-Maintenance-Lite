using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.API.Services;
using PredictiveMaintenance.Data;
using PredictiveMaintenance.Models;

namespace PredictiveMaintenance.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CurrentUserAccessor _currentUser;

    public SensorsController(AppDbContext db, CurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<Sensor>>> GetAll()
    {
        return await _db.Sensors.Include(s => s.Asset).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Sensor>> GetById(int id)
    {
        var sensor = await _db.Sensors
            .Include(s => s.Asset)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sensor == null) return NotFound($"Sensor {id} not found.");
        return sensor;
    }

    [HttpGet("asset/{assetId}")]
    public async Task<ActionResult<List<Sensor>>> GetByAsset(int assetId)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        bool owns = await _db.Assets.AnyAsync(a => a.Id == assetId && a.UserId == userId);
        if (!owns) return NotFound($"Asset {assetId} not found.");

        return await _db.Sensors
            .Where(s => s.UserId == userId && s.Id == assetId)
            .Include(s => s.Asset)
            .ToListAsync();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Sensor updated)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        var sensor = await _db.Sensors
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (sensor == null) return NotFound($"Sensor {id} not found.");

        sensor.SensorName = updated.SensorName;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        var sensor = await _db.Sensors
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (sensor == null) return NotFound($"Sensor {id} not found.");

        _db.Sensors.Remove(sensor);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

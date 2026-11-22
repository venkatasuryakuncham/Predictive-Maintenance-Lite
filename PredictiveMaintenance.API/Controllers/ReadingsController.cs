using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.API.Services;
using PredictiveMaintenance.Data;
using PredictiveMaintenance.Models;

namespace PredictiveMaintenance.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReadingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly MaintenanceService _maintenanceService;
    private readonly CurrentUserAccessor _currentUser;

    public ReadingsController(
        AppDbContext db,
        MaintenanceService maintenanceService,
        CurrentUserAccessor currentUser)
    {
        _db = db;
        _maintenanceService = maintenanceService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? assetId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _db.Readings
            .Include(r => r.Sensor)!
                .ThenInclude(s => s!.Asset)
            .Where(r => r.UserId == userId);

        if (assetId.HasValue)
            query = query.Where(r => r.SensorId == assetId.Value);

        if (from.HasValue)
            query = query.Where(r => r.Timestamp >= from.Value);

        if (to.HasValue)
        {
            var toExclusive = to.Value.TimeOfDay == TimeSpan.Zero
                ? to.Value.AddDays(1)
                : to.Value;
            query = query.Where(r => r.Timestamp < toExclusive);
        }

        var total = await query.CountAsync();

        var readings = await query
            .OrderByDescending(r => r.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            data = readings
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Reading>> GetById(int id)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        var reading = await _db.Readings
            .Include(r => r.Sensor)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (reading == null) return NotFound($"Reading {id} not found.");
        return reading;
    }

    [HttpGet("sensor/{sensorId}")]
    public async Task<ActionResult<object>> GetBySensor(
        int sensorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        bool sensorOwned = await _db.Sensors
            .AnyAsync(s => s.Id == sensorId && s.UserId == userId);
        if (!sensorOwned) return NotFound($"Sensor {sensorId} not found.");

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _db.Readings
            .Where(r => r.UserId == userId && r.SensorId == sensorId);

        var total = await query.CountAsync();

        var readings = await query
            .Include(r => r.Sensor)
            .OrderByDescending(r => r.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            data = readings
        });
    }

    [HttpPost]
    public async Task<ActionResult<Reading>> Create(Reading reading)
    {
        bool sensorExists = await _db.Sensors
            .AnyAsync(s => s.UserId == reading.UserId && s.Id == reading.SensorId);
        if (!sensorExists)
            return BadRequest($"Sensor {reading.SensorId} for user {reading.UserId} does not exist.");

        if (reading.Timestamp == default)
            reading.Timestamp = IstClock.Now;

        _db.Readings.Add(reading);
        await _db.SaveChangesAsync();

        await _maintenanceService.EvaluateThresholdsAsync(reading);

        return CreatedAtAction(nameof(GetById), new { id = reading.Id }, reading);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        var reading = await _db.Readings
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (reading == null) return NotFound($"Reading {id} not found.");

        _db.Readings.Remove(reading);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

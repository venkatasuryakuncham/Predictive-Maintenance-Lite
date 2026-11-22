using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.API.Services;
using PredictiveMaintenance.Data;
using PredictiveMaintenance.Models;

namespace PredictiveMaintenance.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CurrentUserAccessor _currentUser;

    public TicketsController(AppDbContext db, CurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<Ticket>>> GetAll([FromQuery] string? status)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        var query = _db.Tickets
            .Include(t => t.Asset)
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Ticket>> GetById(int id)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        var ticket = await _db.Tickets
            .Include(t => t.Asset)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (ticket == null) return NotFound($"Ticket {id} not found.");
        return ticket;
    }

    [HttpGet("asset/{assetId}")]
    public async Task<ActionResult<List<Ticket>>> GetByAsset(int assetId)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        bool owns = await _db.Assets.AnyAsync(a => a.Id == assetId && a.UserId == userId);
        if (!owns) return NotFound($"Asset {assetId} not found.");

        return await _db.Tickets
            .Where(t => t.AssetId == assetId && t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Ticket>> Create(Ticket ticket)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        bool owns = await _db.Assets.AnyAsync(a => a.Id == ticket.AssetId && a.UserId == userId);
        if (!owns) return BadRequest($"Asset {ticket.AssetId} does not exist.");

        ticket.UserId = userId.Value;
        ticket.CreatedAt = IstClock.Now;
        ticket.Status = "Open";

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    [HttpPatch("{id}/close")]
    public async Task<IActionResult> Close(int id)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (ticket == null) return NotFound($"Ticket {id} not found.");

        if (ticket.Status == "Closed")
            return BadRequest("Ticket is already closed.");

        ticket.Status = "Closed";
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = await _currentUser.GetIdAsync();
        if (userId == null) return Unauthorized();

        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (ticket == null) return NotFound($"Ticket {id} not found.");

        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

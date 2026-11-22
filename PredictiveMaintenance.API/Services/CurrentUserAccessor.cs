using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.Data;
using PredictiveMaintenance.Models;

namespace PredictiveMaintenance.API.Services;

public class CurrentUserAccessor
{
    private readonly IHttpContextAccessor _http;
    private readonly AppDbContext _db;
    private User? _cached;
    private bool _resolved;

    public CurrentUserAccessor(IHttpContextAccessor http, AppDbContext db)
    {
        _http = http;
        _db = db;
    }

    public async Task<User?> GetAsync()
    {
        if (_resolved) return _cached;
        _resolved = true;

        var raw = _http.HttpContext?.Request.Headers["X-User-Id"].ToString();
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (!int.TryParse(raw, out var userId)) return null;

        _cached = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        return _cached;
    }

    public async Task<int?> GetIdAsync() => (await GetAsync())?.Id;
}

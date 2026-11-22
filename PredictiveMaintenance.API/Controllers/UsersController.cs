using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.API.Services;
using PredictiveMaintenance.Data;
using PredictiveMaintenance.Models;

namespace PredictiveMaintenance.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    private readonly CurrentUserAccessor _currentUser;

    public UsersController(AppDbContext db, AuthService auth, CurrentUserAccessor currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public record RegisterRequest(
        string FirstName,
        string LastName,
        string UserId,
        string Email,
        string Password,
        string ConfirmPassword,
        int SecurityQuestion1Id,
        string SecurityAnswer1,
        int SecurityQuestion2Id,
        string SecurityAnswer2);

    public record LoginRequest(string UserIdOrEmail, string Password);

    public record UpdateProfileRequest(string FirstName, string LastName, string UserId, string Email);

    public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);

    public record ForgotLookupRequest(string UserId, string Email);

    public record ForgotVerifyRequest(string UserId, string Email, string Answer1, string Answer2);

    public record ForgotResetRequest(string UserId, string Email, string Answer1, string Answer2, string NewPassword, string ConfirmNewPassword);

    public record UserResponse(int Id, string FirstName, string LastName, string UserId, string Email,
        int SecurityQuestion1Id, int SecurityQuestion2Id);

    private static UserResponse ToResponse(User u) =>
        new(u.Id, u.FirstName, u.LastName, u.UserId, u.Email, u.SecurityQuestion1Id, u.SecurityQuestion2Id);

    private const string PasswordRuleMessage =
        "Password must be at least 8 characters and include an uppercase letter, a lowercase letter, a number, and a special character.";

    private static bool IsValidPassword(string p) =>
        !string.IsNullOrEmpty(p) &&
        p.Length >= 8 &&
        p.Any(char.IsUpper) &&
        p.Any(char.IsLower) &&
        p.Any(char.IsDigit) &&
        p.Any(c => !char.IsLetterOrDigit(c));

    [HttpGet("security-questions")]
    public ActionResult<object> GetSecurityQuestions()
    {
        var questions = SecurityQuestions.All
            .Select((text, idx) => new { id = idx, text })
            .ToList();
        return Ok(questions);
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
            return BadRequest("First and last name are required.");
        if (string.IsNullOrWhiteSpace(req.UserId))
            return BadRequest("User ID is required.");
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest("Email is required.");
        if (!IsValidPassword(req.Password))
            return BadRequest(PasswordRuleMessage);
        if (req.Password != req.ConfirmPassword)
            return BadRequest("Passwords do not match.");
        if (req.SecurityQuestion1Id == req.SecurityQuestion2Id)
            return BadRequest("Please choose two different security questions.");
        if (SecurityQuestions.GetById(req.SecurityQuestion1Id) == null ||
            SecurityQuestions.GetById(req.SecurityQuestion2Id) == null)
            return BadRequest("Invalid security question selection.");
        if (string.IsNullOrWhiteSpace(req.SecurityAnswer1) || string.IsNullOrWhiteSpace(req.SecurityAnswer2))
            return BadRequest("Security answers are required.");

        var userIdLower = req.UserId.Trim();
        var emailLower = req.Email.Trim();

        if (await _db.Users.AnyAsync(u => u.UserId == userIdLower))
            return BadRequest("User ID is already taken.");
        if (await _db.Users.AnyAsync(u => u.Email == emailLower))
            return BadRequest("Email is already registered.");

        var (pwdHash, pwdSalt) = _auth.HashPassword(req.Password);
        var (a1Hash, a1Salt) = _auth.HashAnswer(req.SecurityAnswer1);
        var (a2Hash, a2Salt) = _auth.HashAnswer(req.SecurityAnswer2);

        var user = new User
        {
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            UserId = userIdLower,
            Email = emailLower,
            PasswordHash = pwdHash,
            PasswordSalt = pwdSalt,
            SecurityQuestion1Id = req.SecurityQuestion1Id,
            SecurityAnswer1Hash = a1Hash,
            SecurityAnswer1Salt = a1Salt,
            SecurityQuestion2Id = req.SecurityQuestion2Id,
            SecurityAnswer2Hash = a2Hash,
            SecurityAnswer2Salt = a2Salt,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(ToResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserResponse>> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.UserIdOrEmail) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Credentials are required.");

        var key = req.UserIdOrEmail.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == key || u.Email == key);
        if (user == null || !_auth.VerifyPassword(req.Password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized("Invalid credentials.");

        return Ok(ToResponse(user));
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me()
    {
        var user = await _currentUser.GetAsync();
        if (user == null) return Unauthorized();
        return Ok(ToResponse(user));
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserResponse>> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var user = await _currentUser.GetAsync();
        if (user == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
            return BadRequest("First and last name are required.");
        if (string.IsNullOrWhiteSpace(req.UserId)) return BadRequest("User ID is required.");
        if (string.IsNullOrWhiteSpace(req.Email)) return BadRequest("Email is required.");

        var newUserId = req.UserId.Trim();
        var newEmail = req.Email.Trim();

        if (newUserId != user.UserId &&
            await _db.Users.AnyAsync(u => u.UserId == newUserId && u.Id != user.Id))
            return BadRequest("User ID is already taken.");
        if (newEmail != user.Email &&
            await _db.Users.AnyAsync(u => u.Email == newEmail && u.Id != user.Id))
            return BadRequest("Email is already registered.");

        user.FirstName = req.FirstName.Trim();
        user.LastName = req.LastName.Trim();
        user.UserId = newUserId;
        user.Email = newEmail;
        await _db.SaveChangesAsync();
        return Ok(ToResponse(user));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var user = await _currentUser.GetAsync();
        if (user == null) return Unauthorized();

        if (!IsValidPassword(req.NewPassword))
            return BadRequest(PasswordRuleMessage);
        if (req.NewPassword != req.ConfirmNewPassword)
            return BadRequest("New passwords do not match.");
        if (!_auth.VerifyPassword(req.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            return BadRequest("Current password is incorrect.");

        var (hash, salt) = _auth.HashPassword(req.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("forgot-password/lookup")]
    public async Task<ActionResult<object>> ForgotLookup([FromBody] ForgotLookupRequest req)
    {
        var user = await FindForReset(req.UserId, req.Email);
        if (user == null) return NotFound("No matching account.");

        return Ok(new
        {
            question1 = new { id = user.SecurityQuestion1Id, text = SecurityQuestions.GetById(user.SecurityQuestion1Id) },
            question2 = new { id = user.SecurityQuestion2Id, text = SecurityQuestions.GetById(user.SecurityQuestion2Id) }
        });
    }

    [HttpPost("forgot-password/verify")]
    public async Task<IActionResult> ForgotVerify([FromBody] ForgotVerifyRequest req)
    {
        var user = await FindForReset(req.UserId, req.Email);
        if (user == null) return NotFound("No matching account.");
        if (!_auth.VerifyAnswer(req.Answer1, user.SecurityAnswer1Hash, user.SecurityAnswer1Salt) ||
            !_auth.VerifyAnswer(req.Answer2, user.SecurityAnswer2Hash, user.SecurityAnswer2Salt))
            return BadRequest("Security answers do not match.");
        return NoContent();
    }

    [HttpPost("forgot-password/reset")]
    public async Task<IActionResult> ForgotReset([FromBody] ForgotResetRequest req)
    {
        var user = await FindForReset(req.UserId, req.Email);
        if (user == null) return NotFound("No matching account.");
        if (!_auth.VerifyAnswer(req.Answer1, user.SecurityAnswer1Hash, user.SecurityAnswer1Salt) ||
            !_auth.VerifyAnswer(req.Answer2, user.SecurityAnswer2Hash, user.SecurityAnswer2Salt))
            return BadRequest("Security answers do not match.");
        if (!IsValidPassword(req.NewPassword))
            return BadRequest(PasswordRuleMessage);
        if (req.NewPassword != req.ConfirmNewPassword)
            return BadRequest("New passwords do not match.");

        var (hash, salt) = _auth.HashPassword(req.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<User?> FindForReset(string userId, string email)
    {
        var key1 = (userId ?? "").Trim();
        var key2 = (email ?? "").Trim();
        if (string.IsNullOrEmpty(key1) || string.IsNullOrEmpty(key2)) return null;
        return await _db.Users.FirstOrDefaultAsync(u => u.UserId == key1 && u.Email == key2);
    }
}

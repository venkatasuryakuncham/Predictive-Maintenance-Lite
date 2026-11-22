namespace PredictiveMaintenance.Models;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;

    public int SecurityQuestion1Id { get; set; }
    public string SecurityAnswer1Hash { get; set; } = string.Empty;
    public string SecurityAnswer1Salt { get; set; } = string.Empty;

    public int SecurityQuestion2Id { get; set; }
    public string SecurityAnswer2Hash { get; set; } = string.Empty;
    public string SecurityAnswer2Salt { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}

public static class SecurityQuestions
{
    public static readonly string[] All =
    {
        "What was the name of your first pet?",
        "In what city were you born?",
        "What is your mother's maiden name?",
        "What was the name of your elementary school?",
        "What was the make of your first car?"
    };

    public static string? GetById(int id) =>
        id >= 0 && id < All.Length ? All[id] : null;
}

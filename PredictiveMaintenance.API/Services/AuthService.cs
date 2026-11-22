using System.Security.Cryptography;
using System.Text;

namespace PredictiveMaintenance.API.Services;

public class AuthService
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Iterations = 100_000;

    public (string hash, string salt) HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hashBytes = Pbkdf2(password, saltBytes);
        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
            return false;
        var saltBytes = Convert.FromBase64String(storedSalt);
        var expected = Convert.FromBase64String(storedHash);
        var actual = Pbkdf2(password, saltBytes);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public (string hash, string salt) HashAnswer(string answer)
        => HashPassword(Normalize(answer));

    public bool VerifyAnswer(string answer, string storedHash, string storedSalt)
        => VerifyPassword(Normalize(answer), storedHash, storedSalt);

    private static string Normalize(string s) => (s ?? "").Trim().ToLowerInvariant();

    private static byte[] Pbkdf2(string input, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(input ?? ""),
            salt,
            Iterations,
            HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(HashSizeBytes);
    }
}

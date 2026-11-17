using System.Security.Cryptography;
using SupplyChain.Domain.Abstractions;

namespace SupplyChain.Infrastructure.Security;

/// <summary>
/// PBKDF2-HMAC-SHA256 password hashing with a per-password salt.
/// Format: <c>pbkdf2-sha256$iterations$salt$key</c> (all base64 except the scheme).
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Scheme = "pbkdf2-sha256";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int DefaultIterations = 210_000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, DefaultIterations, Algorithm, KeySize);

        return string.Join('$',
            Scheme,
            DefaultIterations.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(key));
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        var parts = hash.Split('$');
        if (parts.Length != 4 || !string.Equals(parts[0], Scheme, StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

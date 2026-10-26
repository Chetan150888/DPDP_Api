using System.Security.Cryptography;

namespace DPDP_Api.Security;

public sealed class PasswordService
{
    private const int Iterations = 120000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public (string Salt, string Hash) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        return (Convert.ToBase64String(salt), Convert.ToBase64String(derive.GetBytes(HashSize)));
    }

    public bool Verify(string password, string saltBase64, string hashBase64)
    {
        var salt = Convert.FromBase64String(saltBase64);
        var expected = Convert.FromBase64String(hashBase64);
        using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        return CryptographicOperations.FixedTimeEquals(derive.GetBytes(HashSize), expected);
    }

    public static bool MeetsPolicy(string password) =>
        password.Length >= 12 && password.Any(char.IsUpper) && password.Any(char.IsLower) &&
        password.Any(char.IsDigit) && password.Any(c => !char.IsLetterOrDigit(c));
}

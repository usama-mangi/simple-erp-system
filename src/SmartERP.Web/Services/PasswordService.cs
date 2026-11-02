namespace SmartERP.Web.Services;

public static class PasswordService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        byte[] salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, System.Security.Cryptography.HashAlgorithmName.SHA256, HashSize);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 2) return false;
        byte[] salt, expected;
        try { salt = Convert.FromBase64String(parts[0]); expected = Convert.FromBase64String(parts[1]); }
        catch { return false; }
        byte[] actual = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, System.Security.Cryptography.HashAlgorithmName.SHA256, HashSize);
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
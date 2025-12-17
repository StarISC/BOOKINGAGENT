using System.Security.Cryptography;

namespace BookingAgent.App.Services;

public interface IPasswordHasher
{
    bool Verify(string password, byte[] salt, byte[] expectedHash);
}

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 200_000;
    private const int HashSizeBytes = 32;

    public bool Verify(string password, byte[] salt, byte[] expectedHash)
    {
        if (salt is null || salt.Length == 0 || expectedHash is null || expectedHash.Length == 0)
        {
            return false;
        }

        var pwd = System.Text.Encoding.UTF8.GetBytes(password);
        var computed = Rfc2898DeriveBytes.Pbkdf2(pwd, salt, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
        return CryptographicOperations.FixedTimeEquals(computed, expectedHash);
    }
}

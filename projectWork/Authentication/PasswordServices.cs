using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace projectWork.Authentication;

public class PasswordServices
{
    private byte[] GenerateSalt(int length = 16)
    {
        var salt = new byte[length];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    private byte[] HashPassword(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));

        argon2.Salt = salt;
        argon2.DegreeOfParallelism = 2;
        argon2.MemorySize = 16384;
        argon2.Iterations = 4;

        return argon2.GetBytes(32);
    }

    public (string hash, string salt) CreateHash(string password)
    {
        var salt = GenerateSalt();
        var hash = HashPassword(password, salt);

        return (
            Convert.ToBase64String(hash),
            Convert.ToBase64String(salt)
        );
    }

    public bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var salt = Convert.FromBase64String(storedSalt);
        var hash = HashPassword(password, salt);
        var hashToCheck = Convert.ToBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hashToCheck),
            Encoding.UTF8.GetBytes(storedHash)
        );
    }
}

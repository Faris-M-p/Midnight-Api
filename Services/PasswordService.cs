using System.Security.Cryptography;
using System.Text;

namespace MidnightApi.Services;

using MidnightApi.Services.Interfaces;

public class PasswordService : IPasswordService
{
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = HashWithSalt(password, salt);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.', 2);
        if (parts.Length != 2)
        {
            // Legacy SHA256 hex (pre-auth refactor) — keep login working for old hashes if any remain.
            var legacy = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
            return string.Equals(legacy, storedHash, StringComparison.OrdinalIgnoreCase);
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = HashWithSalt(password, salt);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] HashWithSalt(string password, byte[] salt)
    {
        return SHA256.HashData(Combine(salt, Encoding.UTF8.GetBytes(password)));
    }

    private static byte[] Combine(byte[] left, byte[] right)
    {
        var result = new byte[left.Length + right.Length];
        Buffer.BlockCopy(left, 0, result, 0, left.Length);
        Buffer.BlockCopy(right, 0, result, left.Length, right.Length);
        return result;
    }
}

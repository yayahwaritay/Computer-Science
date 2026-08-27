using System.Security.Cryptography;

namespace CompSci.Core.Services;

public static class PasswordGenerator
{
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string AllChars = Uppercase + Lowercase + Digits;

    /// <summary>
    /// Generates a random temporary password satisfying AuthValidator's password policy
    /// (min length, at least one uppercase, one lowercase and one digit).
    /// </summary>
    public static string GenerateTemporaryPassword(int length = 12)
    {
        Span<char> password = stackalloc char[length];

        // Guarantee at least one of each required character class.
        password[0] = Uppercase[RandomNumberGenerator.GetInt32(Uppercase.Length)];
        password[1] = Lowercase[RandomNumberGenerator.GetInt32(Lowercase.Length)];
        password[2] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];

        for (var i = 3; i < length; i++)
            password[i] = AllChars[RandomNumberGenerator.GetInt32(AllChars.Length)];

        // Shuffle so the guaranteed characters aren't always in the same positions.
        for (var i = length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}

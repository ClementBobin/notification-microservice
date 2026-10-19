using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Genkan.Auth.Business;

/// <summary>PBKDF2-SHA256, versioned format "v1.iterations.salt.hash" so the cost can be raised later.</summary>
public static class PasswordHasher
{
    private const int Iterations = 600_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"v1.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var p = stored.Split('.');
        if (p.Length != 4 || p[0] != "v1" || !int.TryParse(p[1], out var iterations)) return false;
        var salt = Convert.FromBase64String(p[2]);
        var expected = Convert.FromBase64String(p[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

public static class Base32
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Encode(byte[] data)
    {
        var sb = new StringBuilder();
        int buffer = 0, bits = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                sb.Append(Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
            buffer &= (1 << bits) - 1;
        }
        if (bits > 0) sb.Append(Alphabet[(buffer << (5 - bits)) & 31]);
        return sb.ToString();
    }

    public static byte[] Decode(string text)
    {
        var result = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in text.TrimEnd('=').ToUpperInvariant())
        {
            var v = Alphabet.IndexOf(c);
            if (v < 0) throw new FormatException("Invalid base32 character");
            buffer = (buffer << 5) | v;
            bits += 5;
            if (bits >= 8)
            {
                result.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
            buffer &= (1 << bits) - 1;
        }
        return result.ToArray();
    }
}

/// <summary>RFC 6238 TOTP (SHA-1, 6 digits, 30 s) compatible with Google/Microsoft Authenticator.</summary>
public static class Totp
{
    public static string NewSecret() => Base32.Encode(RandomNumberGenerator.GetBytes(20));

    public static string BuildUri(string issuer, string account, string secret) =>
        $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&digits=6&period=30";

    private static string Code(byte[] key, long counter)
    {
        Span<byte> msg = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(msg, counter);
        Span<byte> hash = stackalloc byte[20];
        HMACSHA1.HashData(key, msg, hash);
        var o = hash[19] & 0x0F;
        var bin = ((hash[o] & 0x7F) << 24) | (hash[o + 1] << 16) | (hash[o + 2] << 8) | hash[o + 3];
        return (bin % 1_000_000).ToString("D6");
    }

    public static bool Verify(string secret, string? code)
    {
        if (code is not { Length: 6 }) return false;
        var key = Base32.Decode(secret);
        var step = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var given = Encoding.ASCII.GetBytes(code);
        for (var drift = -1; drift <= 1; drift++)
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Code(key, step + drift)), given))
                return true;
        return false;
    }
}

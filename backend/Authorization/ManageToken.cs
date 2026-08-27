using System.Security.Cryptography;

namespace KampusKayipEsya.Api.Authorization;

/// <summary>
/// F0 geçici sahip jetonu. 128-bit rastgele değer, SHA-256 hash olarak saklanır.
/// F1-BE-04 bu mekanizmayı gerçek yetkilendirme ile değiştirir.
/// </summary>
public static class ManageToken
{
    public const string HeaderName = "X-Manage-Token";
    public const int ByteLength = 16;
    public const int HashLength = 32;
    public const string RequiredError = "A valid manage token is required.";

    public static string Create(out byte[] hash)
    {
        var bytes = RandomNumberGenerator.GetBytes(ByteLength);
        hash = SHA256.HashData(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool Matches(string? presented, byte[]? storedHash)
    {
        if (storedHash is null || storedHash.Length != HashLength)
        {
            return false;
        }

        if (!TryDecode(presented, out var raw))
        {
            // Sabit süreli karşılaştırmayı yine de çalıştır; erken dönüş jeton biçimini sızdırmasın.
            CryptographicOperations.FixedTimeEquals(storedHash, new byte[HashLength]);
            return false;
        }

        var computed = SHA256.HashData(raw);
        return CryptographicOperations.FixedTimeEquals(computed, storedHash);
    }

    private static bool TryDecode(string? presented, out byte[] raw)
    {
        raw = [];
        if (string.IsNullOrWhiteSpace(presented))
        {
            return false;
        }

        var trimmed = presented.Trim();
        if (trimmed.Length != ByteLength * 2)
        {
            return false;
        }

        try
        {
            raw = Convert.FromHexString(trimmed);
            return raw.Length == ByteLength;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

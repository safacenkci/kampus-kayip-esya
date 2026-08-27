namespace KampusKayipEsya.Api.Models;

public static class ItemRules
{
    public const string KindLost = "lost";
    public const string KindFound = "found";

    public const string StatusOpen = "open";
    public const string StatusClaimed = "claimed";
    public const string StatusClosed = "closed";

    public static readonly string[] Kinds = [KindLost, KindFound];
    public static readonly string[] Statuses = [StatusOpen, StatusClaimed, StatusClosed];

    public static readonly string[] Locations =
    [
        "merkez",
        "kütüphane",
        "yemekhane",
        "mühendislik",
        "yurt",
        "spor salonu"
    ];

    public static readonly string[] Categories =
    [
        "öğrenci kartı",
        "anahtar",
        "telefon",
        "çanta",
        "kıyafet",
        "kulaklık",
        "diğer"
    ];

    public static string LocationError =>
        "Invalid location. Allowed values: " + string.Join(", ", Locations) + ".";

    public static string CategoryError =>
        "Invalid category. Allowed values: " + string.Join(", ", Categories) + ".";

    public static string KindError => "Invalid kind. Allowed values: lost, found.";

    public static string StatusError => "Invalid status. Allowed values: open, claimed, closed.";

    public static bool TryNormalizeKind(string? value, out string kind)
    {
        kind = (value ?? string.Empty).Trim().ToLowerInvariant();
        return kind is KindLost or KindFound;
    }

    public static bool TryNormalizeStatus(string? value, out string status)
    {
        status = (value ?? string.Empty).Trim().ToLowerInvariant();
        return status is StatusOpen or StatusClaimed or StatusClosed;
    }

    public static bool TryNormalizeLocation(string? value, out string location) =>
        TryMatchAllowed(value, Locations, out location);

    public static bool TryNormalizeCategory(string? value, out string category) =>
        TryMatchAllowed(value, Categories, out category);

    public static string OppositeKind(string kind) =>
        kind == KindLost ? KindFound : KindLost;

    private static bool TryMatchAllowed(string? value, string[] allowed, out string canonical)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            canonical = trimmed;
            return false;
        }

        foreach (var option in allowed)
        {
            if (string.Equals(option, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                canonical = option;
                return true;
            }
        }

        canonical = trimmed;
        return false;
    }
}

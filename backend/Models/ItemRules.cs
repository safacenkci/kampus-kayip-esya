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

    public static readonly string[] DefaultCategories =
    [
        "giyim",
        "elektronik",
        "belgeler",
        "aksesuar",
        "kitap",
        "diğer"
    ];

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
}

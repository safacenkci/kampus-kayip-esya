using System.Globalization;

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

    private static readonly CompareInfo TurkishCompare =
        CultureInfo.GetCultureInfo("tr-TR").CompareInfo;

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
        TryCanonical(value, Locations, out location);

    public static bool TryNormalizeCategory(string? value, out string category) =>
        TryCanonical(value, Categories, out category);

    public static bool RevealsContact(string? status) =>
        status is StatusClaimed or StatusClosed;

    public static bool EqualsIgnoreCaseTr(string? left, string? right) =>
        left is not null
        && right is not null
        && TurkishCompare.Compare(left, right, CompareOptions.IgnoreCase) == 0;

    public static Item ForClient(Item item)
    {
        return new Item
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            Location = item.Location,
            Category = item.Category,
            Contact = RevealsContact(item.Status) ? item.Contact : null,
            PhotoUrl = item.PhotoUrl,
            Kind = item.Kind,
            Status = item.Status,
            CreatedAt = item.CreatedAt,
            StatusHistory = item.StatusHistory
                .OrderBy(h => h.Timestamp)
                .ThenBy(h => h.Id)
                .ToList()
        };
    }

    public static void RecordStatus(Item item, string status, DateTime? timestamp = null)
    {
        item.Status = status;
        item.StatusHistory.Add(new StatusHistoryEntry
        {
            Status = status,
            Timestamp = timestamp ?? DateTime.UtcNow
        });
    }

    private static bool TryCanonical(string? value, string[] allowed, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        foreach (var option in allowed)
        {
            if (EqualsIgnoreCaseTr(option, trimmed))
            {
                canonical = option;
                return true;
            }
        }

        return false;
    }
}

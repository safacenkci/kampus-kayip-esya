namespace KampusKayipEsya.Api.Models;

public static class ItemMapper
{
    public static ItemResponse ToResponse(Item item, bool includeHistory = false)
    {
        return new ItemResponse
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            Location = item.Location,
            Category = item.Category,
            Contact = item.Status == ItemRules.StatusOpen ? null : item.Contact,
            PhotoUrl = item.PhotoUrl,
            Kind = item.Kind,
            Status = item.Status,
            CreatedAt = item.CreatedAt,
            StatusHistory = includeHistory ? MapHistory(item) : null
        };
    }

    public static IReadOnlyList<ItemResponse> ToListResponse(IEnumerable<Item> items) =>
        items.Select(item => ToResponse(item)).ToList();

    private static List<StatusHistoryDto> MapHistory(Item item) =>
        item.StatusHistory
            .OrderBy(h => h.ChangedAt)
            .ThenBy(h => h.Id)
            .Select(h => new StatusHistoryDto
            {
                From = h.FromStatus,
                To = h.ToStatus,
                ChangedAt = h.ChangedAt
            })
            .ToList();
}

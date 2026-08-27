using KampusKayipEsya.Api.Authorization;

namespace KampusKayipEsya.Api.Models;

public static class ItemMapper
{
    public static ItemResponse ToResponse(
        Item item,
        bool includeHistory = false,
        bool isItemDetail = false,
        bool hasValidManageToken = false,
        string? manageToken = null)
    {
        var revealContact = ContactVisibilityPolicy.CanRevealContact(isItemDetail, hasValidManageToken);

        return new ItemResponse
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            Location = item.Location,
            Category = item.Category,
            Contact = revealContact ? item.Contact : null,
            PhotoUrl = item.PhotoUrl,
            Kind = item.Kind,
            Status = item.Status,
            CreatedAt = item.CreatedAt,
            ManageToken = manageToken,
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

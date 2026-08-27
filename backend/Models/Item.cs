using System.Text.Json.Serialization;

namespace KampusKayipEsya.Api.Models;

public class Item
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Category { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Contact { get; set; }

    public string? PhotoUrl { get; set; }
    public string Kind { get; set; } = ItemRules.KindLost;
    public string Status { get; set; } = ItemRules.StatusOpen;
    public DateTime CreatedAt { get; set; }
    public List<StatusHistoryEntry> StatusHistory { get; set; } = [];
}

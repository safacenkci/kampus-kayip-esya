using System.Text.Json.Serialization;

namespace KampusKayipEsya.Api.Models;

public class StatusHistoryEntry
{
    [JsonIgnore]
    public int Id { get; set; }

    [JsonIgnore]
    public int ItemId { get; set; }

    public string Status { get; set; } = ItemRules.StatusOpen;

    public DateTime Timestamp { get; set; }
}

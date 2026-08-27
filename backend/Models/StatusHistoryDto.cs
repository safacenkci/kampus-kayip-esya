namespace KampusKayipEsya.Api.Models;

public class StatusHistoryDto
{
    public string? From { get; set; }
    public string To { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}

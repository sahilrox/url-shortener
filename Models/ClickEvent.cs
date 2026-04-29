namespace UrlShortener.API.Models;

public class ClickEvent
{
    public int Id { get; set; }
    public string ShortCode { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string Country { get; set; } = "Unknown";
    public string Device { get; set; } = "Unknown";
}
namespace UrlShortener.API.Models;

public class UrlMapping
{
    public long Id { get; set; }
    public string ShortCode { get; set; } = string.Empty;
    public string LongUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public int HitCount { get; set; } = 0;
}
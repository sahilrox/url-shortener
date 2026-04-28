namespace UrlShortener.API.Models;

public class ShortenRequest
{
    public string Url { get; set; } = string.Empty;
    public string? CustomCode { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
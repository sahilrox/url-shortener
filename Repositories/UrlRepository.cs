using UrlShortener.API.Data;
using UrlShortener.API.Models;
using Microsoft.EntityFrameworkCore;

namespace UrlShortener.API.Repositories;

public class UrlRepository
{
    private readonly AppDbContext _context;

    public UrlRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UrlMapping> CreateAsync(UrlMapping url)
    {
        _context.Urls.Add(url);
        await _context.SaveChangesAsync();
        return url;
    }

    public async Task<UrlMapping?> GetByCodeAsync(string code)
    {
        return await _context.Urls
            .FirstOrDefaultAsync(x => x.ShortCode == code);
    }
}
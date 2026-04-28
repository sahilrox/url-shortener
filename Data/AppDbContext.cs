using Microsoft.EntityFrameworkCore;
using UrlShortener.API.Models;

namespace UrlShortener.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<UrlMapping> Urls { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UrlMapping>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Index on ShortCode for fast lookups
            entity.HasIndex(e => e.ShortCode).IsUnique();

            entity.Property(e => e.ShortCode)
                  .IsRequired()
                  .HasMaxLength(10);

            entity.Property(e => e.LongUrl)
                  .IsRequired();
        });
    }
}
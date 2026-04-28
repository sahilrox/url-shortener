using Microsoft.EntityFrameworkCore;
using UrlShortener.API.Data;
using UrlShortener.API.Repositories;
using UrlShortener.API.Helpers;
using UrlShortener.API.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres")
    ));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

    if (!string.IsNullOrEmpty(databaseUrl))
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');

        var connectionString =
            $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
    }
});

builder.Services.AddScoped<UrlRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.MapGet("/", () => "URL Shortener API is running 🚀");

// POST /shorten — placeholder for now
app.MapPost("/shorten", async (UrlRepository repo, ShortenRequest request, HttpContext httpContext) =>
{
    // ✅ Validate URL
    if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        return Results.BadRequest("Invalid URL");

    string code;

    var reservedCodes = new[] { "help", "admin", "login", "stats" };

    if (!string.IsNullOrWhiteSpace(request.CustomCode))
    {
        if (reservedCodes.Contains(request.CustomCode.ToLower()))
            return Results.BadRequest("This code is reserved");
    }

    // ✅ Custom code validation
    if (!string.IsNullOrWhiteSpace(request.CustomCode))
    {
        if (!Regex.IsMatch(request.CustomCode, "^[a-zA-Z0-9]+$"))
            return Results.BadRequest("Custom code must be alphanumeric");

        if (request.CustomCode.Length > 10)
            return Results.BadRequest("Custom code too long");

        var exists = await repo.GetByCodeAsync(request.CustomCode);
        if (exists != null)
            return Results.BadRequest("Custom code already exists");

        code = request.CustomCode;
    }
    else
    {
        // ✅ Generate unique code
        do
        {
            code = ShortCodeGenerator.Generate();
        }
        while (await repo.GetByCodeAsync(code) != null);
    }

    if (!string.IsNullOrWhiteSpace(request.CustomCode))
    {
        if (request.CustomCode == "string")
            return Results.BadRequest("Please provide a valid custom code");

        if (!Regex.IsMatch(request.CustomCode, "^[a-zA-Z0-9]+$"))
            return Results.BadRequest("Custom code must be alphanumeric");

        if (request.CustomCode.Length > 10)
            return Results.BadRequest("Custom code too long");
    }

    var entity = new UrlMapping
    {
        ShortCode = code,
        LongUrl = request.Url,
        ExpiresAt = request.ExpiresAt
    };

    await repo.CreateAsync(entity);

    var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

    return Results.Ok(new
    {
        shortUrl = $"{baseUrl}/{code}",
        code,
        originalUrl = request.Url,
        expiresAt = request.ExpiresAt
    });
});

// GET /{code} — placeholder for now  
app.MapGet("/{code}", async (
    string code,
    UrlRepository repo,
    IMemoryCache cache,
    AppDbContext db) =>
{
    var cacheKey = $"url_{code}";

    if (!cache.TryGetValue(cacheKey, out UrlMapping? url))
    {
        url = await repo.GetByCodeAsync(code);

        if (url != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(2)
            };

            cache.Set(cacheKey, url, cacheOptions);
        }
    }

    if (url == null)
        return Results.NotFound();

    if (url.ExpiresAt != null && url.ExpiresAt < DateTime.UtcNow)
        return Results.NotFound("Link expired");

    url.HitCount++;
    await db.SaveChangesAsync();

    return Results.Redirect(url.LongUrl);
});

app.MapGet("/stats/{code}", async (string code, UrlRepository repo) =>
{
    var url = await repo.GetByCodeAsync(code);

    if (url == null)
        return Results.NotFound();

    return Results.Ok(new
    {
        shortCode = url.ShortCode,
        originalUrl = url.LongUrl,
        totalClicks = url.HitCount,
        createdAt = url.CreatedAt,
        expiresAt = url.ExpiresAt,
        isExpired = url.ExpiresAt != null && url.ExpiresAt < DateTime.UtcNow
    });
});

app.Run();
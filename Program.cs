using Microsoft.EntityFrameworkCore;
using UrlShortener.API.Data;
using UrlShortener.API.Repositories;
using UrlShortener.API.Helpers;
using UrlShortener.API.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);


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

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;              // 100 requests
        opt.Window = TimeSpan.FromMinutes(1); // per minute
        opt.QueueLimit = 0;
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

    if (string.IsNullOrWhiteSpace(databaseUrl))
        throw new Exception("DATABASE_URL is NOT set ❌");

    Console.WriteLine("DATABASE_URL FOUND ✅");

    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');

    var port = uri.Port > 0 ? uri.Port : 5432;

    var connectionString =
        $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

    options.UseNpgsql(connectionString);
});
builder.Services.AddScoped<UrlRepository>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");

    var options = ConfigurationOptions.Parse(redisUrl);
    options.AbortOnConnectFail = false;

    return ConnectionMultiplexer.Connect(options);
});

var app = builder.Build();
app.UseRateLimiter();


using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        Console.WriteLine("Migration completed ✅");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Migration failed ❌");
        Console.WriteLine(ex.Message);
    }
}


app.UseSwagger();
app.UseSwaggerUI();

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

    if (!string.IsNullOrWhiteSpace(request.CustomCode))
    {        

        if (reservedCodes.Contains(request.CustomCode.ToLower()))
            return Results.BadRequest("This code is reserved");

        if (request.CustomCode == "string")
            return Results.BadRequest("Please provide a valid custom code");

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
        do
        {
            code = ShortCodeGenerator.Generate();
        }
        while (await repo.GetByCodeAsync(code) != null);
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
})
.RequireRateLimiting("fixed");

// GET /{code} — placeholder for now  
app.MapGet("/{code}", async (
    string code,
    UrlRepository repo,
    IConnectionMultiplexer redis,
    AppDbContext db) =>
{
    var cache = redis.GetDatabase();
    var cacheKey = $"url:{code}";

    string? longUrl = await cache.StringGetAsync(cacheKey);

    UrlMapping? url = null;

    if (!string.IsNullOrEmpty(longUrl))
    {
        url = new UrlMapping { ShortCode = code, LongUrl = longUrl };
    }
    else
    {
        url = await repo.GetByCodeAsync(code);

        if (url != null)
        {
            await cache.StringSetAsync(cacheKey, url.LongUrl, TimeSpan.FromMinutes(10));
        }
    }

    if (url == null)
        return Results.NotFound();

    if (url.ExpiresAt != null && url.ExpiresAt < DateTime.UtcNow)
        return Results.NotFound("Link expired");

    url.HitCount++;
    await db.SaveChangesAsync();

    return Results.Redirect(url.LongUrl);
})
.RequireRateLimiting("fixed");

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
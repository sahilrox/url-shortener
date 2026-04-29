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

    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        Console.WriteLine("Using Render DB ✅");

        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        var port = uri.Port > 0 ? uri.Port : 5432;

        var connectionString =
            $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

        options.UseNpgsql(connectionString);
    }
    else
    {
        Console.WriteLine("Using LOCAL DB");

        options.UseNpgsql("Host=localhost;Port=5432;Database=urlshortener;Username=postgres;Password=sahil1999");
    }
});
builder.Services.AddScoped<UrlRepository>();

// builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
// {
//     var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");

//     var uri = new Uri(redisUrl);
//     var userInfo = uri.UserInfo.Split(':');

//     var options = new ConfigurationOptions
//     {
//         EndPoints = { { uri.Host, uri.Port } },

//         User = userInfo[0],
//         Password = userInfo.Length > 1 ? userInfo[1] : null,

//         // 🔥 REQUIRED FOR RENDER
//         Ssl = true,
//         AbortOnConnectFail = false,

//         // 🔥 ADD THESE (critical)
//         AllowAdmin = true,
//         ConnectRetry = 3,
//         ConnectTimeout = 5000,
//         SyncTimeout = 5000
//     };

//     return ConnectionMultiplexer.Connect(options);
// });

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
        return Results.BadRequest(new { error = "Invalid URL" });

    string code;

    var reservedCodes = new[] { "help", "admin", "login", "stats" };


    if (!string.IsNullOrWhiteSpace(request.CustomCode))
    {        

        if (reservedCodes.Contains(request.CustomCode.ToLower()))
            return Results.BadRequest(new { error = "This code is reserved" });

        if (request.CustomCode == "string")
            return Results.BadRequest(new { error = "Please provide a valid custom code" });

        if (!Regex.IsMatch(request.CustomCode, "^[a-zA-Z0-9]+$"))
            return Results.BadRequest(new { error = "Custom code must be alphanumeric" });

        if (request.CustomCode.Length > 10)
            return Results.BadRequest(new { error = "Custom code too long" });

        var exists = await repo.GetByCodeAsync(request.CustomCode);
        if (exists != null)
            return Results.BadRequest(new { error = "Custom code already exists" });

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

    var baseUrl = "https://url-shortener-f45d.onrender.com";

    return Results.Ok(new
    {
        shortUrl = $"{baseUrl}/{code}",
        code,
        originalUrl = request.Url,
        expiresAt = request.ExpiresAt
    });
})
.RequireRateLimiting("fixed");

app.MapGet("/stats/{code}", async (string code, AppDbContext db) =>
{
    var url = await db.Urls.FirstOrDefaultAsync(x => x.ShortCode == code);

    if (url == null)
        return Results.NotFound();

    var totalClicks = await db.ClickEvents
        .CountAsync(c => c.ShortCode == code);

    var recentClicks = await db.ClickEvents
        .Where(c => c.ShortCode == code)
        .OrderByDescending(c => c.Timestamp)
        .Take(10)
        .ToListAsync();


    var clicksByDate = await db.ClickEvents
        .Where(c => c.ShortCode == code)
        .GroupBy(c => c.Timestamp.Date)
        .Select(g => new
        {
            date = g.Key,
            count = g.Count()
        })
        .OrderBy(x => x.date)
        .ToListAsync();

    return Results.Ok(new
    {
        url.ShortCode,
        url.LongUrl,
        totalClicks,
        recentClicks,
        clicksByDate
    });
});

// GET /{code} — placeholder for now  
app.MapGet("/{code:regex(^[a-zA-Z0-9]+$)}", async (
    HttpContext context,
    string code,
    UrlRepository repo,
    AppDbContext db) =>
{
    Console.WriteLine($"🔥 Redirect request for code: {code}");

    var url = await repo.GetByCodeAsync(code);

    if (url == null)
        return Results.NotFound(new { error = "URL not found" });

    if (url.ExpiresAt != null && url.ExpiresAt < DateTime.UtcNow)
        return Results.BadRequest(new { error = "Link expired" });

    try
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        db.ClickEvents.Add(new ClickEvent
        {
            ShortCode = code,
            Timestamp = DateTime.UtcNow,
            IpAddress = ip
        });

        url.HitCount++;

        await db.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Analytics error: {ex.Message}");
    }

    return Results.Redirect(url.LongUrl, false);
});


app.Run();
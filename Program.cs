using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UrlShortener.API.Data;
using UrlShortener.API.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"];

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("❌ Database connection string not found");
}

connectionString += ";SSL Mode=Require;Trust Server Certificate=true";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// JWT
var key = "THIS_IS_A_SUPER_SECRET_KEY_12345";

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 🔥 Auto-migrate (important for Render)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "API Running");

// ================= AUTH =================

app.MapPost("/register", async (AuthRequest req, AppDbContext db) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return Results.BadRequest("Email and password required");

        var exists = await db.Users.AnyAsync(u => u.Email == req.Email);
        if (exists)
            return Results.BadRequest("User already exists");

        var user = new User
        {
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Results.Ok("Registered successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Register error: {ex}");
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/login", async (AuthRequest req, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);

    if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        return Results.BadRequest("Invalid credentials");

    var token = new JwtSecurityToken(
        claims: new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        },
        expires: DateTime.UtcNow.AddDays(1),
        signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256Signature)
    );

    return Results.Ok(new
    {
        token = new JwtSecurityTokenHandler().WriteToken(token)
    });
});

// ================= SHORTEN =================

app.MapPost("/shorten", async (
    HttpContext context,
    UrlRequest req,
    AppDbContext db) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null)
        return Results.Unauthorized();

    var code = req.CustomCode ?? Guid.NewGuid().ToString()[..6];

    var url = new UrlMapping
    {
        ShortCode = code,
        LongUrl = req.Url,
        CreatedAt = DateTime.UtcNow,
        UserId = int.Parse(userId)
    };

    db.Urls.Add(url);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        shortUrl = $"https://url-shortener-f45d.onrender.com/{code}",
        code
    });
}).RequireAuthorization();

// ================= REDIRECT =================

app.MapGet("/{code:regex(^[a-zA-Z0-9]+$)}", async (
    HttpContext context,
    string code,
    AppDbContext db) =>
{
    var url = await db.Urls.FirstOrDefaultAsync(x => x.ShortCode == code);

    if (url == null)
        return Results.NotFound();

    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    if (ip == "::1" || ip == "127.0.0.1")
        ip = "8.8.8.8";

    string country = "Unknown";

    try
    {
        using var client = new HttpClient();
        var geo = await client.GetFromJsonAsync<IpResponse>($"http://ip-api.com/json/{ip}");

        if (geo != null && geo.status == "success")
            country = geo.country;
    }
    catch { }

    var userAgent = context.Request.Headers["User-Agent"].ToString().ToLower();

    string device = "Desktop";
    if (userAgent.Contains("mobile") || userAgent.Contains("android") || userAgent.Contains("iphone"))
        device = "Mobile";
    else if (userAgent.Contains("tablet"))
        device = "Tablet";

    db.ClickEvents.Add(new ClickEvent
    {
        ShortCode = code,
        Timestamp = DateTime.UtcNow,
        IpAddress = ip,
        Country = country,
        Device = device
    });

    url.HitCount++;

    await db.SaveChangesAsync();

    return Results.Redirect(url.LongUrl);
});

app.MapGet("/debug-db", async (AppDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();

        // Check if Users table exists
        var exists = await db.Database.ExecuteSqlRawAsync(@"
            SELECT 1 FROM information_schema.tables 
            WHERE table_name = 'Users';
        ");

        return Results.Ok(new
        {
            canConnect,
            usersTableExists = exists == 1
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// ================= STATS =================

app.MapGet("/stats/{code}", async (
    HttpContext context,
    string code,
    string? range,
    AppDbContext db) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null)
        return Results.Unauthorized();

    DateTime fromDate = DateTime.MinValue;

    if (range == "24h") fromDate = DateTime.UtcNow.AddHours(-24);
    else if (range == "7d") fromDate = DateTime.UtcNow.AddDays(-7);
    else if (range == "30d") fromDate = DateTime.UtcNow.AddDays(-30);

    var totalClicks = await db.ClickEvents
        .Where(c => c.ShortCode == code && c.Timestamp >= fromDate)
        .CountAsync();

    var recentClicks = await db.ClickEvents
        .Where(c => c.ShortCode == code && c.Timestamp >= fromDate)
        .OrderByDescending(c => c.Timestamp)
        .Take(10)
        .Select(c => new
        {
            timestamp = c.Timestamp,
            country = c.Country,
            device = c.Device
        })
        .ToListAsync();

    var clicksByDate = await db.ClickEvents
        .Where(c => c.ShortCode == code && c.Timestamp >= fromDate)
        .GroupBy(c => c.Timestamp.Date)
        .Select(g => new { date = g.Key, count = g.Count() })
        .ToListAsync();

    var clicksByCountry = await db.ClickEvents
        .Where(c => c.ShortCode == code && c.Timestamp >= fromDate)
        .GroupBy(c => c.Country ?? "Unknown")
        .Select(g => new { country = g.Key, count = g.Count() })
        .ToListAsync();

    var clicksByDevice = await db.ClickEvents
        .Where(c => c.ShortCode == code && c.Timestamp >= fromDate)
        .GroupBy(c => c.Device ?? "Unknown")
        .Select(g => new { device = g.Key, count = g.Count() })
        .ToListAsync();

    return Results.Ok(new
    {
        totalClicks,
        recentClicks,
        clicksByDate,
        clicksByCountry,
        clicksByDevice
    });
}).RequireAuthorization();

app.Run();

// ================= DTOs =================

public class AuthRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class UrlRequest
{
    public string Url { get; set; }
    public string? CustomCode { get; set; }
}

public class IpResponse
{
    public string status { get; set; }
    public string country { get; set; }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortener;
using SQLitePCL;
using System.Threading.RateLimiting;

Batteries.Init();
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source = UrlsDb"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Too many requests. Try again later.", token);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(ip))
            ip = Guid.NewGuid().ToString();

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"ip:{ip}",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            });
    });
});



var app = builder.Build();


app.MapPost("/shorten", GetShortUrl);
app.MapGet("/{code}", RedirectToOriginal);

static async Task<IResult> GetShortUrl(AppDbContext _context, [FromBody] ShortenRequest request, HttpContext httpContext)
{
    if (!Uri.TryCreate(request.LongUrl, UriKind.Absolute, out _))
    {
        return Results.BadRequest("The URL is invalid.");
    }
    var generator = new GenerateUniqueCode(_context);
    var code = await generator.GenerateCodeAsync();

    var url = new Url
    {
        LongUrl = request.LongUrl,
        Code = code,
        ShortUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{code}",
        CreatedAt = DateTime.UtcNow,
    };

    _context.Add(url);
    await _context.SaveChangesAsync();

    return TypedResults.Ok(url);
}

static async Task<IResult> RedirectToOriginal(string code, AppDbContext _context)
{
    var url = await _context.Urls.FirstOrDefaultAsync(c => c.Code == code);
    return url is null ? TypedResults.NotFound() : TypedResults.Redirect(url.LongUrl);
}

app.UseHttpsRedirection();

app.Run();

public record ShortenRequest(string LongUrl);
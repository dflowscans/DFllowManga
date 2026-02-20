using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace MangaReader.Middleware;

public class AntiScrapingMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        // Only protect manga images in /uploads/manga/
        if (path != null && path.StartsWith("/uploads/manga/", StringComparison.OrdinalIgnoreCase))
        {
            var referer = context.Request.Headers["Referer"].ToString();
            var host = context.Request.Host.Host;

            // If there's a referer, it must be from our own domain
            if (!string.IsNullOrEmpty(referer))
            {
                try
                {
                    var refererUri = new Uri(referer);
                    if (refererUri.Host != host && refererUri.Host != "localhost")
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return;
                    }
                }
                catch
                {
                    // Invalid referer format
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }
            else
            {
                // No referer - direct access or scraper
                // We could allow direct access for browsers, but most scrapers don't send referers.
                // For better security, we block empty referers for manga images.
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }

        await _next(context);
    }
}

using MangaReader.Data;
using Microsoft.EntityFrameworkCore;

namespace MangaReader.Middleware;

public class MaintenanceModeMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Skip check for static files and essential auth/admin paths
        if (path.StartsWith("/css") || 
            path.StartsWith("/js") || 
            path.StartsWith("/lib") || 
            path.StartsWith("/images") ||
            path.StartsWith("/favicon.ico"))
        {
            await _next(context);
            return;
        }

        bool isMaintenance = false;
        bool dbError = false;

        try
        {
            // Try to check maintenance mode setting
            var setting = await dbContext.SiteSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == "MaintenanceMode");
            isMaintenance = setting?.Value == "true";
        }
        catch (Exception)
        {
            // Database connection issue or table missing
            isMaintenance = true;
            dbError = true;
        }

        if (isMaintenance)
        {
            var isAdmin = context.User?.FindFirst("IsAdmin")?.Value == "True";
            
            // Paths that are always accessible even in maintenance mode
            bool isAllowedPath = path.StartsWith("/home/maintenance") || 
                                path.StartsWith("/auth/login") ||
                                path.StartsWith("/auth/logout") ||
                                (isAdmin && (path.StartsWith("/admin/managedatabase") || 
                                            path.StartsWith("/admin/togglemaintenance") ||
                                            path.StartsWith("/admin/fixdatabase")));

            if (!isAllowedPath)
            {
                if (dbError)
                {
                    // If it's a DB error, we can't really do much but show a static-ish maintenance page
                    context.Response.Redirect("/Home/Maintenance?error=db");
                }
                else
                {
                    context.Response.Redirect("/Home/Maintenance");
                }
                return;
            }
        }

        await _next(context);
    }
}

using MangaReader.Data;
using MangaReader.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var mysqlHost = Environment.GetEnvironmentVariable("MYSQL_HOST");
if (string.IsNullOrWhiteSpace(mysqlHost))
{
    mysqlHost = "YOUR_DATABASE_HOST_IP"; // e.g., "127.0.0.1"
    Environment.SetEnvironmentVariable("MYSQL_HOST", mysqlHost);
}

var mysqlPort = Environment.GetEnvironmentVariable("MYSQL_PORT");
if (string.IsNullOrWhiteSpace(mysqlPort))
{
    mysqlPort = "3306";
    Environment.SetEnvironmentVariable("MYSQL_PORT", mysqlPort);
}

var mysqlDb = Environment.GetEnvironmentVariable("MYSQL_DB");
if (string.IsNullOrWhiteSpace(mysqlDb))
{
    mysqlDb = "YOUR_DATABASE_NAME";
    Environment.SetEnvironmentVariable("MYSQL_DB", mysqlDb);
}

var mysqlUser = Environment.GetEnvironmentVariable("MYSQL_USER");
if (string.IsNullOrWhiteSpace(mysqlUser))
{
    mysqlUser = "YOUR_DATABASE_USER";
    Environment.SetEnvironmentVariable("MYSQL_USER", mysqlUser);
}

var mysqlPassword = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
if (string.IsNullOrWhiteSpace(mysqlPassword))
{
    mysqlPassword = "YOUR_DATABASE_PASSWORD";
    Environment.SetEnvironmentVariable("MYSQL_PASSWORD", mysqlPassword);
}

var connectionString =
    $"Server={mysqlHost};Port={mysqlPort};Database={mysqlDb};User={mysqlUser};Password={mysqlPassword};" +
    "TreatTinyAsBoolean=true;AllowPublicKeyRetrieval=True;SslMode=Preferred";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 29))));

// Increase file upload limit
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100 MB
});

// Register application services
builder.Services.AddScoped<IBookmarkService, BookmarkService>();
builder.Services.AddScoped<IChapterService, ChapterService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Add authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
    });

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Add Razor Pages support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Database initialization/migration helper
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        using var command = context.Database.GetDbConnection().CreateCommand();
        await context.Database.OpenConnectionAsync();
        
        // Ensure AniListId column exists in Mangas table
        command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Mangas' AND COLUMN_NAME = 'AniListId' AND TABLE_SCHEMA = DATABASE();";
        var result = await command.ExecuteScalarAsync();
        if (Convert.ToInt32(result) == 0)
        {
            command.CommandText = "ALTER TABLE Mangas ADD COLUMN AniListId INT NULL;";
            await command.ExecuteNonQueryAsync();
        }

        // Add FollowChangelog to Users table
        try
        {
            command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'FollowChangelog' AND TABLE_SCHEMA = DATABASE();";
            var followColResult = await command.ExecuteScalarAsync();
            if (Convert.ToInt32(followColResult) == 0)
            {
                command.CommandText = "ALTER TABLE Users ADD COLUMN FollowChangelog TINYINT(1) NOT NULL DEFAULT 1;";
                await command.ExecuteNonQueryAsync();
            }
        }
        catch { /* Might already exist */ }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

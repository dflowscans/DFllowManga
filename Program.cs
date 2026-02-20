using System.Data.Common;
using MangaReader.Data;
using MangaReader.Models;
using MangaReader.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using MangaReader.Middleware;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? $"Server={Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "YOUR_DATABASE_HOST_IP"};" +
       $"Port={Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306"};" +
       $"Database={Environment.GetEnvironmentVariable("MYSQL_DB") ?? "YOUR_DATABASE_NAME"};" +
       $"User={Environment.GetEnvironmentVariable("MYSQL_USER") ?? "YOUR_DATABASE_USER"};" +
       $"Password={Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? "YOUR_DATABASE_PASSWORD"};" +
       "TreatTinyAsBoolean=true;AllowPublicKeyRetrieval=True;SslMode=Preferred;Connect Timeout=60;Default Command Timeout=60";

var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, serverVersion, mysqlOptions =>
    {
        mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600L;
});


builder.Services.AddScoped<IBookmarkService, BookmarkService>();
builder.Services.AddScoped<IChapterService, ChapterService>();
builder.Services.AddScoped<INotificationService, NotificationService>();


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
    });

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        try 
        {
            
             var preConn = context.Database.GetDbConnection();
             if (preConn.State != System.Data.ConnectionState.Open) await preConn.OpenAsync();
             using (var preCmd = preConn.CreateCommand())
             {
                 
                 preCmd.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = '__EFMigrationsHistory' AND TABLE_SCHEMA = DATABASE();";
                 if (Convert.ToInt32(await preCmd.ExecuteScalarAsync()) > 0)
                 {
                     var migrationsToCleanup = new Dictionary<string, (string[] Tables, (string Table, string Column)[] Columns)>
                     {
                         { "20251229134954_AddLevelingAndDecorations", (new[] { "ChapterViews", "PfpDecorations" }, new[] { ("Users", "XP"), ("Users", "Level"), ("Users", "EquippedDecorationId") }) },
                         { "20251229140324_AddUserTitles", (new[] { "UserTitles" }, new[] { ("Users", "EquippedTitleId") }) },
                         { "20251229153050_AddNotificationsAndReactions", (new[] { "Notifications", "CommentReactions" }, Array.Empty<(string, string)>()) },
                         { "20251229163701_AddLockingAndHideReadingListExplicitJoin", (new[] { "UserUnlockedDecoration", "UserUnlockedTitle" }, new[] { ("UserTitles", "IsLocked"), ("PfpDecorations", "IsLocked"), ("Users", "HideReadingList") }) }
                     };

                     foreach (var migration in migrationsToCleanup)
                     {
                         preCmd.CommandText = $"SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId = '{migration.Key}';";
                         if (Convert.ToInt32(await preCmd.ExecuteScalarAsync()) == 0)
                         {
                             
                             preCmd.CommandText = "SET FOREIGN_KEY_CHECKS = 0;";
                             await preCmd.ExecuteNonQueryAsync();

                             foreach (var table in migration.Value.Tables)
                             {
                                 
                                 
                                 
                                 
                             }

                             foreach (var col in migration.Value.Columns)
                             {
                                 try
                                 {
                                     preCmd.CommandText = $"SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = '{col.Table}' AND COLUMN_NAME = '{col.Column}' AND TABLE_SCHEMA = DATABASE();";
                                     if (Convert.ToInt32(await preCmd.ExecuteScalarAsync()) > 0)
                                     {
                                         
                                         
                                         
                                     }
                                 }
                                 catch (Exception ex) { logger.LogWarning(ex, $"Failed to drop column {col.Column} from {col.Table} table."); }
                             }

                             preCmd.CommandText = "SET FOREIGN_KEY_CHECKS = 1;";
                             await preCmd.ExecuteNonQueryAsync();
                             logger.LogInformation($"Cleaned up orphan objects for migration {migration.Key}.");
                         }
                     }
                 }
             }

            await context.Database.MigrateAsync();
            logger.LogInformation("Entity Framework migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Entity Framework migrations failed. Proceeding with manual database fixes.");
        }

        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();
        
        using var command = connection.CreateCommand();
        
        command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Mangas' AND COLUMN_NAME = 'AniListId' AND TABLE_SCHEMA = DATABASE();";
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
        {
            command.CommandText = "ALTER TABLE Mangas ADD COLUMN AniListId INT NULL;";
            await command.ExecuteNonQueryAsync();
        }
        
        command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Mangas' AND COLUMN_NAME = 'IsSuggestive' AND TABLE_SCHEMA = DATABASE();";
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
        {
            command.CommandText = "ALTER TABLE Mangas ADD COLUMN IsSuggestive TINYINT(1) NOT NULL DEFAULT 0;";
            await command.ExecuteNonQueryAsync();
            logger.LogInformation("Added missing column IsSuggestive to Mangas table.");
        }

        command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Mangas' AND COLUMN_NAME = 'BannerPositionX' AND TABLE_SCHEMA = DATABASE();";
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
        {
            command.CommandText = "ALTER TABLE Mangas ADD COLUMN BannerPositionX INT NOT NULL DEFAULT 50, ADD COLUMN BannerPositionY INT NOT NULL DEFAULT 50, ADD COLUMN HasTitleShadow TINYINT(1) NOT NULL DEFAULT 0, ADD COLUMN TitleShadowSize INT NOT NULL DEFAULT 12, ADD COLUMN TitleShadowOpacity DOUBLE NOT NULL DEFAULT 0.8;";
            await command.ExecuteNonQueryAsync();
            logger.LogInformation("Added missing banner and shadow columns to Mangas table.");
        }

        command.CommandText = "ALTER TABLE Chapters MODIFY COLUMN Title VARCHAR(300) NULL;";
        await command.ExecuteNonQueryAsync();

        
        command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'ChapterComments' AND TABLE_SCHEMA = DATABASE();";
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
        {
            command.CommandText = @"
                CREATE TABLE ChapterComments (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    ChapterId INT NOT NULL,
                    UserId INT NOT NULL,
                    Content LONGTEXT NOT NULL,
                    CreatedAt DATETIME(6) NOT NULL,
                    ParentCommentId INT NULL,
                    RepliedToUserId INT NULL,
                    CONSTRAINT FK_ChapterComments_Chapters_ChapterId FOREIGN KEY (ChapterId) REFERENCES Chapters(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_ChapterComments_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            await command.ExecuteNonQueryAsync();
            logger.LogInformation("Created missing ChapterComments table.");
        }
        else
        {
            
            command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'ChapterComments' AND COLUMN_NAME = 'ParentCommentId' AND TABLE_SCHEMA = DATABASE();";
            if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
            {
                command.CommandText = "ALTER TABLE ChapterComments ADD COLUMN ParentCommentId INT NULL;";
                await command.ExecuteNonQueryAsync();
                try {
                    command.CommandText = "ALTER TABLE ChapterComments ADD CONSTRAINT FK_ChapterComments_ChapterComments_ParentCommentId FOREIGN KEY (ParentCommentId) REFERENCES ChapterComments(Id) ON DELETE CASCADE;";
                    await command.ExecuteNonQueryAsync();
                } catch {}
                logger.LogInformation("Added missing ParentCommentId column to ChapterComments table.");
            }

            
            command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'ChapterComments' AND COLUMN_NAME = 'RepliedToUserId' AND TABLE_SCHEMA = DATABASE();";
            if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
            {
                command.CommandText = "ALTER TABLE ChapterComments ADD COLUMN RepliedToUserId INT NULL;";
                await command.ExecuteNonQueryAsync();
                try {
                    command.CommandText = "ALTER TABLE ChapterComments ADD CONSTRAINT FK_ChapterComments_Users_RepliedToUserId FOREIGN KEY (RepliedToUserId) REFERENCES Users(Id) ON DELETE SET NULL;";
                    await command.ExecuteNonQueryAsync();
                } catch {}
                logger.LogInformation("Added missing RepliedToUserId column to ChapterComments table.");
            }
        }

        command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'SiteSettings' AND TABLE_SCHEMA = DATABASE();";
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
        {
            command.CommandText = @"
                CREATE TABLE SiteSettings (
                    `Key` VARCHAR(100) NOT NULL,
                    `Value` LONGTEXT NOT NULL,
                    PRIMARY KEY (`Key`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            await command.ExecuteNonQueryAsync();
            logger.LogInformation("Created missing SiteSettings table.");
        }

        command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'Notifications' AND TABLE_SCHEMA = DATABASE();";
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
        {
            command.CommandText = @"
                CREATE TABLE Notifications (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    UserId INT NOT NULL,
                    Type INT NOT NULL,
                    Message VARCHAR(500) NOT NULL,
                    IsRead TINYINT(1) NOT NULL DEFAULT 0,
                    CreatedAt DATETIME(6) NOT NULL,
                    RelatedMangaId INT NULL,
                    RelatedChapterId INT NULL,
                    RelatedCommentId INT NULL,
                    TriggerUserId INT NULL,
                    CONSTRAINT FK_Notifications_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_Notifications_Users_TriggerUserId FOREIGN KEY (TriggerUserId) REFERENCES Users(Id) ON DELETE SET NULL,
                    CONSTRAINT FK_Notifications_Mangas_MangaId FOREIGN KEY (RelatedMangaId) REFERENCES Mangas(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_Notifications_Chapters_ChapterId FOREIGN KEY (RelatedChapterId) REFERENCES Chapters(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_Notifications_ChapterComments_CommentId FOREIGN KEY (RelatedCommentId) REFERENCES ChapterComments(Id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            await command.ExecuteNonQueryAsync();
            logger.LogInformation("Created missing Notifications table.");
        }

        
        var tablesToVerify = new Dictionary<string, string>
        {
            { "ChapterViews", "CREATE TABLE ChapterViews (Id INT AUTO_INCREMENT PRIMARY KEY, UserId INT NOT NULL, ChapterId INT NOT NULL, ViewedAt DATETIME(6) NOT NULL, IPAddress VARCHAR(100) NULL, UserAgent TEXT NULL, FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;" },
            { "PfpDecorations", "CREATE TABLE PfpDecorations (Id INT AUTO_INCREMENT PRIMARY KEY, Name VARCHAR(255) NOT NULL, ImageUrl VARCHAR(500) NOT NULL, Description TEXT NULL, IsLocked TINYINT(1) NOT NULL DEFAULT 0, CreatedAt DATETIME(6) NOT NULL) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;" },
            { "UserTitles", "CREATE TABLE UserTitles (Id INT AUTO_INCREMENT PRIMARY KEY, Name VARCHAR(255) NOT NULL, Description TEXT NULL, IsLocked TINYINT(1) NOT NULL DEFAULT 0, CreatedAt DATETIME(6) NOT NULL) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;" },
            { "UserUnlockedDecoration", "CREATE TABLE UserUnlockedDecoration (Id INT AUTO_INCREMENT PRIMARY KEY, UserId INT NOT NULL, DecorationId INT NOT NULL, UnlockedAt DATETIME(6) NOT NULL, CONSTRAINT FK_UserUnlockedDecoration_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE, CONSTRAINT FK_UserUnlockedDecoration_Decorations_DecId FOREIGN KEY (DecorationId) REFERENCES PfpDecorations(Id) ON DELETE CASCADE, UNIQUE KEY IX_UserUnlockedDecoration_UserId_DecorationId (UserId, DecorationId)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;" },
            { "UserUnlockedTitle", "CREATE TABLE UserUnlockedTitle (Id INT AUTO_INCREMENT PRIMARY KEY, UserId INT NOT NULL, TitleId INT NOT NULL, UnlockedAt DATETIME(6) NOT NULL, CONSTRAINT FK_UserUnlockedTitle_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE, CONSTRAINT FK_UserUnlockedTitle_Titles_TitleId FOREIGN KEY (TitleId) REFERENCES UserTitles(Id) ON DELETE CASCADE, UNIQUE KEY IX_UserUnlockedTitle_UserId_TitleId (UserId, TitleId)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;" },
            { "CommentReactions", "CREATE TABLE CommentReactions (Id INT AUTO_INCREMENT PRIMARY KEY, CommentId INT NOT NULL, UserId INT NOT NULL, IsLike TINYINT(1) NOT NULL, CreatedAt DATETIME(6) NOT NULL, CONSTRAINT FK_CommentReactions_ChapterComments_CommentId FOREIGN KEY (CommentId) REFERENCES ChapterComments(Id) ON DELETE CASCADE, CONSTRAINT FK_CommentReactions_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE, UNIQUE KEY IX_CommentReactions_CommentId_UserId (CommentId, UserId)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;" }
        };

        foreach (var table in tablesToVerify)
        {
            command.CommandText = $"SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = '{table.Key}' AND TABLE_SCHEMA = DATABASE();";
            if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
            {
                try {
                    command.CommandText = table.Value;
                    await command.ExecuteNonQueryAsync();
                    logger.LogInformation($"Created missing table {table.Key}.");
                } catch (Exception ex) {
                    logger.LogWarning($"Could not create table {table.Key}: {ex.Message}");
                }
            }
        }

        
        command.CommandText = "SELECT COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Users' AND TABLE_SCHEMA = DATABASE();";
        var existingUserColumns = new HashSet<string>();
        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                existingUserColumns.Add(reader.GetString(0));
            }
        }

        var columnsToAdd = new Dictionary<string, string>
        {
            { "IsSubAdmin", "TINYINT(1) NOT NULL DEFAULT 0" },
            { "AvatarUrl", "VARCHAR(500) NULL" },
            { "ProfilePicture", "VARCHAR(500) NULL" },
            { "Bio", "TEXT NULL" },
            { "XP", "INT NOT NULL DEFAULT 0" },
            { "Level", "INT NOT NULL DEFAULT 1" },
            { "EquippedDecorationId", "INT NULL" },
            { "EquippedTitleId", "INT NULL" },
            { "HideReadingList", "TINYINT(1) NOT NULL DEFAULT 0" },
            { "FollowChangelog", "TINYINT(1) NOT NULL DEFAULT 1" },
            { "CustomPrimaryColor", "VARCHAR(50) NULL" },
            { "CustomAccentColor", "VARCHAR(50) NULL" },
            { "CustomCss", "TEXT NULL" },
            { "DisableFeaturedBanners", "TINYINT(1) NOT NULL DEFAULT 0" },
            { "ShowAllFeaturedAsCovers", "TINYINT(1) NOT NULL DEFAULT 0" },
            { "CustomBackgroundUrl", "VARCHAR(500) NULL" },
            { "SiteOpacity", "DOUBLE NULL DEFAULT 1.0" },
            { "IgnoreSuggestiveWarnings", "TINYINT(1) NOT NULL DEFAULT 0" }
        };

        foreach (var col in columnsToAdd)
        {
            if (!existingUserColumns.Contains(col.Key))
            {
                command.CommandText = $"ALTER TABLE Users ADD COLUMN {col.Key} {col.Value};";
                await command.ExecuteNonQueryAsync();
                logger.LogInformation($"Added missing column {col.Key} to Users table.");
            }
        }

        
        command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'UserBookmarks' AND COLUMN_NAME = 'AddedAt' AND TABLE_SCHEMA = DATABASE();";
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
        {
            command.CommandText = "ALTER TABLE UserBookmarks ADD COLUMN AddedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6);";
            await command.ExecuteNonQueryAsync();
            logger.LogInformation("Added missing column AddedAt to UserBookmarks table.");
        }

        
        command.CommandText = "SELECT COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_NAME = 'CommentReactions' AND TABLE_SCHEMA = DATABASE();";
        var existingCommentReactionColumns = new HashSet<string>();
        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                existingCommentReactionColumns.Add(reader.GetString(0));
            }
        }

        var commentReactionColumnsToAdd = new Dictionary<string, string>
        {
            { "CommentId", "INT NOT NULL" },
            { "UserId", "INT NOT NULL" },
            { "IsLike", "TINYINT(1) NOT NULL" },
            { "CreatedAt", "DATETIME(6) NOT NULL" }
        };

        foreach (var col in commentReactionColumnsToAdd)
        {
            if (!existingCommentReactionColumns.Contains(col.Key))
            {
                command.CommandText = $"ALTER TABLE CommentReactions ADD COLUMN {col.Key} {col.Value};";
                await command.ExecuteNonQueryAsync();
                logger.LogInformation($"Added missing column {col.Key} to CommentReactions table.");
            }
        }

        
        command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'Notifications' AND TABLE_SCHEMA = DATABASE();";
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) > 0)
        {
            
            command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Notifications' AND COLUMN_NAME = 'Type' AND TABLE_SCHEMA = DATABASE();";
            if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
            {
                command.CommandText = "ALTER TABLE Notifications ADD COLUMN Type INT NOT NULL DEFAULT 0;";
                await command.ExecuteNonQueryAsync();
                logger.LogInformation("Added missing column Type to Notifications table.");
            }
            else
            {
                
                command.CommandText = "ALTER TABLE Notifications MODIFY COLUMN Type INT NOT NULL DEFAULT 0;";
                await command.ExecuteNonQueryAsync();
            }

            
            var notifCols = new Dictionary<string, string>
            {
                { "RelatedMangaId", "INT NULL" },
                { "RelatedChapterId", "INT NULL" },
                { "RelatedCommentId", "INT NULL" },
                { "TriggerUserId", "INT NULL" }
            };

            foreach (var col in notifCols)
            {
                command.CommandText = $"SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Notifications' AND COLUMN_NAME = '{col.Key}' AND TABLE_SCHEMA = DATABASE();";
                if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
                {
                    command.CommandText = $"ALTER TABLE Notifications ADD COLUMN {col.Key} {col.Value};";
                    await command.ExecuteNonQueryAsync();
                    logger.LogInformation($"Added missing column {col.Key} to Notifications table.");
                }
            }
        }

        
        command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'CommentReactions' AND TABLE_SCHEMA = DATABASE();";
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) > 0)
        {
            
            command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'CommentReactions' AND COLUMN_NAME = 'Type' AND TABLE_SCHEMA = DATABASE();";
            if (Convert.ToInt32(await command.ExecuteScalarAsync()) > 0)
            {
                
                
                command.CommandText = "ALTER TABLE CommentReactions MODIFY COLUMN Type INT NOT NULL DEFAULT 0;";
                await command.ExecuteNonQueryAsync();
                logger.LogInformation("Fixed unwanted Type column in CommentReactions table by adding default value.");
            }
        }

        logger.LogInformation("Database initialization and fixes completed.");

        
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.IsAdmin);
        if (adminUser == null)
        {
            logger.LogInformation("No admin user found. Creating default admin user...");
            var defaultAdmin = new User
            {
                Username = "Admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                IsAdmin = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(defaultAdmin);
            await context.SaveChangesAsync();
            logger.LogInformation("Default admin user created. Username: Admin, Password: Admin123!");
        }
        else
        {
            logger.LogInformation("Admin user exists: {Username}", adminUser.Username);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database initialization.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<AntiScrapingMiddleware>();
app.UseMiddleware<MaintenanceModeMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

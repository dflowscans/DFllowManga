using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MangaReader.Data;
using MangaReader.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MangaReader.Controllers;

public class AuthController(ApplicationDbContext context, ILogger<AuthController> logger) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<AuthController> _logger = logger;

    public class DecorationRequest
    {
        public int DecorationId { get; set; }
    }

    public class TitleRequest
    {
        public int TitleId { get; set; }
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Username and password are required.");
            return View();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            _logger.LogWarning("Failed login attempt for username: {Username}", username);
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("IsAdmin", user.IsAdmin.ToString()),
            new Claim("IsSubAdmin", user.IsSubAdmin.ToString()),
            new Claim("ProfilePicture", user.ProfilePicture ?? "https://wallpapers-clan.com/wp-content/uploads/2022/08/default-pfp-24.jpg")
        };

        var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
        };

        await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties);
        _logger.LogInformation("User {Username} logged in successfully.", username);

        return user.IsAdmin ? RedirectToAction("Index", "Admin") : RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string username, string password, string confirmPassword)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Username and password are required.");
            return View();
        }

        if (password != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Passwords do not match.");
            return View();
        }

        if (username.Length < 3 || username.Length > 100)
        {
            ModelState.AddModelError("username", "Username must be between 3 and 100 characters.");
            return View();
        }

        if (password.Length < 6)
        {
            ModelState.AddModelError("password", "Password must be at least 6 characters.");
            return View();
        }

        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            ModelState.AddModelError("username", "Username already exists.");
            return View();
        }

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsAdmin = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _logger.LogInformation("New user registered: {Username}", username);

        return RedirectToAction("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
        {
            ModelState.AddModelError(string.Empty, "All fields are required.");
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, "New passwords do not match.");
            return View();
        }

        if (newPassword.Length < 6)
        {
            ModelState.AddModelError("newPassword", "New password must be at least 6 characters.");
            return View();
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            ModelState.AddModelError("currentPassword", "Current password is incorrect.");
            return View();
        }

        if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
        {
            ModelState.AddModelError("newPassword", "New password cannot be the same as the current password.");
            return View();
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _logger.LogInformation("User {Username} changed password.", user.Username);

        TempData["SuccessMessage"] = "Password changed successfully.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> PublicProfile(int id)
    {
        var user = await _context.Users
            .Include(u => u.EquippedDecoration)
            .Include(u => u.EquippedTitle)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        var bookmarks = await _context.UserBookmarks
            .Include(b => b.Manga)
            .Where(b => b.UserId == id)
            .ToListAsync();

        ViewBag.User = user;
        ViewBag.Bookmarks = user.HideReadingList ? new List<UserBookmark>() : bookmarks;
        ViewBag.ReadingStats = new
        {
            Total = bookmarks.Count,
            Reading = bookmarks.Count(b => b.Status == BookmarkStatus.Reading),
            Completed = bookmarks.Count(b => b.Status == BookmarkStatus.Completed),
            OnHold = bookmarks.Count(b => b.Status == BookmarkStatus.OnHold),
            Dropped = bookmarks.Count(b => b.Status == BookmarkStatus.Dropped),
            PlanToRead = bookmarks.Count(b => b.Status == BookmarkStatus.PlanToRead)
        };
        ViewBag.TotalComments = await _context.ChapterComments.CountAsync(c => c.UserId == id);

        return View();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile(string? status = null)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users
            .Include(u => u.EquippedDecoration)
            .Include(u => u.EquippedTitle)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return RedirectToAction("Login");
        }

        int currentLevelXp = (int)(100.0 * Math.Pow(1.5, user.Level - 1));
        ViewBag.XpProgress = (double)user.XP / currentLevelXp * 100.0;
        ViewBag.XpToNextLevel = currentLevelXp;

        var settings = await _context.SiteSettings
            .Where(s => s.Key == "EnableDecorations" || s.Key == "EnableTitles")
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        ViewBag.EnableDecorations = settings.GetValueOrDefault("EnableDecorations", "true");
        ViewBag.EnableTitles = settings.GetValueOrDefault("EnableTitles", "true");

        status = status?.Trim();
        ViewBag.User = user;
        ViewBag.AvailableDecorations = await _context.PfpDecorations.OrderBy(d => d.LevelRequirement).ToListAsync();
        ViewBag.AvailableTitles = await _context.UserTitles.OrderBy(t => t.LevelRequirement).ToListAsync();
        ViewBag.UnlockedDecorations = await _context.UserUnlockedDecorations
            .Where(ud => ud.UserId == userId)
            .ToListAsync();
        ViewBag.UnlockedTitles = await _context.UserUnlockedTitles
            .Where(ut => ut.UserId == userId)
            .ToListAsync();

        var bookmarksQuery = _context.UserBookmarks.Include(b => b.Manga).Where(b => b.UserId == userId);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookmarkStatus>(status, true, out var bookmarkStatus))
        {
            bookmarksQuery = bookmarksQuery.Where(b => b.Status == bookmarkStatus);
        }

        var bookmarks = await bookmarksQuery.OrderByDescending(b => b.UpdatedAt).ToListAsync();
        ViewBag.Bookmarks = bookmarks;
        ViewBag.CurrentStatus = status;
        
        var stats = await _context.UserBookmarks
            .Where(b => b.UserId == userId)
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        ViewBag.AllCount = stats.Sum(s => s.Count);
        ViewBag.PlanToReadCount = stats.FirstOrDefault(s => s.Status == BookmarkStatus.PlanToRead)?.Count ?? 0;
        ViewBag.ReadingCount = stats.FirstOrDefault(s => s.Status == BookmarkStatus.Reading)?.Count ?? 0;
        ViewBag.CompletedCount = stats.FirstOrDefault(s => s.Status == BookmarkStatus.Completed)?.Count ?? 0;
        ViewBag.OnHoldCount = stats.FirstOrDefault(s => s.Status == BookmarkStatus.OnHold)?.Count ?? 0;
        ViewBag.DroppedCount = stats.FirstOrDefault(s => s.Status == BookmarkStatus.Dropped)?.Count ?? 0;

        return View();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ExportAniListXml()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var bookmarks = await _context.UserBookmarks
            .Include(b => b.Manga)
            .Where(b => b.UserId == userId)
            .ToListAsync();

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement("myanimelist",
                new XElement("myinfo",
                    new XElement("user_id", user.Id),
                    new XElement("user_name", user.Username),
                    new XElement("user_export_type", "2"),
                    new XElement("user_total_manga", bookmarks.Count),
                    new XElement("user_total_reading", bookmarks.Count(b => b.Status == BookmarkStatus.Reading)),
                    new XElement("user_total_completed", bookmarks.Count(b => b.Status == BookmarkStatus.Completed)),
                    new XElement("user_total_onhold", bookmarks.Count(b => b.Status == BookmarkStatus.OnHold)),
                    new XElement("user_total_dropped", bookmarks.Count(b => b.Status == BookmarkStatus.Dropped)),
                    new XElement("user_total_plantoread", bookmarks.Count(b => b.Status == BookmarkStatus.PlanToRead))
                ),
                bookmarks.Select(b =>
                {
                    string statusStr = b.Status switch
                    {
                        BookmarkStatus.Reading => "Reading",
                        BookmarkStatus.Completed => "Completed",
                        BookmarkStatus.OnHold => "On-Hold",
                        BookmarkStatus.Dropped => "Dropped",
                        BookmarkStatus.PlanToRead => "Plan to Read",
                        _ => "Reading"
                    };

                    return new XElement("manga",
                        new XElement("manga_mangadb_id", b.Manga?.AniListId ?? 0),
                        new XElement("series_title", new XCData(b.Manga?.Title ?? "")),
                        new XElement("series_type", "Manga"),
                        new XElement("series_chapters", "0"),
                        new XElement("my_id", "0"),
                        new XElement("my_read_chapters", "0"),
                        new XElement("my_status", statusStr),
                        new XElement("my_last_updated", ((DateTimeOffset)b.UpdatedAt).ToUnixTimeSeconds())
                    );
                })
            )
        );

        var bytes = Encoding.UTF8.GetBytes(xml.ToString());
        return File(bytes, "application/xml", $"{user.Username}_AniList_Export.xml");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportAniListXml(IFormFile xmlFile)
    {
        if (xmlFile == null || xmlFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a valid XML file.";
            return RedirectToAction("Profile");
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        try
        {
            using var stream = xmlFile.OpenReadStream();
            var doc = XDocument.Load(stream);
            var mangaElements = doc.Root?.Elements("manga") ?? Enumerable.Empty<XElement>();
            
            int importedCount = 0;
            int updatedCount = 0;
            int skippedCount = 0;

            foreach (var element in mangaElements)
            {
                var aniListIdStr = element.Element("manga_mangadb_id")?.Value;
                if (int.TryParse(aniListIdStr, out var aniListId))
                {
                    var manga = await _context.Mangas.FirstOrDefaultAsync(m => m.AniListId == aniListId);
                    if (manga != null)
                    {
                        var statusStr = element.Element("my_status")?.Value;
                        var status = statusStr switch
                        {
                            "Reading" => BookmarkStatus.Reading,
                            "Completed" => BookmarkStatus.Completed,
                            "On-Hold" => BookmarkStatus.OnHold,
                            "Dropped" => BookmarkStatus.Dropped,
                            "Plan to Read" => BookmarkStatus.PlanToRead,
                            _ => BookmarkStatus.Reading
                        };

                        var existingBookmark = await _context.UserBookmarks
                            .FirstOrDefaultAsync(b => b.UserId == userId && b.MangaId == manga.Id);

                        if (existingBookmark == null)
                        {
                            _context.UserBookmarks.Add(new UserBookmark
                            {
                                UserId = userId,
                                MangaId = manga.Id,
                                Status = status,
                                AddedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            });
                            importedCount++;
                        }
                        else
                        {
                            existingBookmark.Status = status;
                            existingBookmark.UpdatedAt = DateTime.UtcNow;
                            updatedCount++;
                        }
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Import completed: {importedCount} imported, {updatedCount} updated, {skippedCount} skipped (manga not found).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing AniList XML");
            TempData["ErrorMessage"] = "An error occurred while processing the XML file.";
        }

        return RedirectToAction("Profile");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(IFormFile? pfp, string? bio, bool hideReadingList, bool disableFeaturedBanners, bool showAllFeaturedAsCovers, bool ignoreSuggestiveWarnings, string? customPrimaryColor, string? customAccentColor, string? customBackgroundColor, string? customNavbarColor, string? customTextColor, string? customBackgroundUrl, double? siteOpacity, string? customCss)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (pfp != null && pfp.Length > 0)
        {
            try
            {
                if (pfp.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "Profile picture must be less than 5MB.";
                    return RedirectToAction("Profile");
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                var extension = Path.GetExtension(pfp.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] = "Invalid file type. Allowed: JPG, PNG, WEBP, GIF.";
                    return RedirectToAction("Profile");
                }

                var fileName = $"{userId}_{DateTime.UtcNow.Ticks}{extension}";
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pfps");
                
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await pfp.CopyToAsync(stream);
                }
                user.ProfilePicture = $"/uploads/pfps/{fileName}";
                _logger.LogInformation("User {UserId} updated profile picture to {PfpPath}", userId, user.ProfilePicture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading PFP for user {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred while uploading your profile picture.";
                return RedirectToAction("Profile");
            }
        }

        user.Bio = bio;
        user.HideReadingList = hideReadingList;
        user.DisableFeaturedBanners = disableFeaturedBanners;
        user.ShowAllFeaturedAsCovers = showAllFeaturedAsCovers;
        user.IgnoreSuggestiveWarnings = ignoreSuggestiveWarnings;
        user.CustomPrimaryColor = customPrimaryColor;
        user.CustomAccentColor = customAccentColor;
        user.CustomBackgroundColor = customBackgroundColor;
        user.CustomNavbarColor = customNavbarColor;
        user.CustomTextColor = customTextColor;
        user.CustomBackgroundUrl = customBackgroundUrl;
        user.SiteOpacity = siteOpacity;
        user.CustomCss = customCss;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Profile updated successfully.";
        return RedirectToAction("Profile");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ResetAppearance()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.CustomPrimaryColor = null;
        user.CustomAccentColor = null;
        user.CustomBackgroundColor = null;
        user.CustomNavbarColor = null;
        user.CustomTextColor = null;
        user.CustomBackgroundUrl = null;
        user.SiteOpacity = 1.0;
        user.CustomCss = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Appearance settings reset to default.";
        return RedirectToAction("Profile");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ExportCss()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.CustomCss)) return NotFound();

        var bytes = Encoding.UTF8.GetBytes(user.CustomCss);
        return File(bytes, "text/css", $"{user.Username}_custom.css");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ImportCss(IFormFile cssFile)
    {
        if (cssFile == null || cssFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a valid CSS file.";
            return RedirectToAction("Profile");
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        using (var reader = new StreamReader(cssFile.OpenReadStream()))
        {
            user.CustomCss = await reader.ReadToEndAsync();
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Custom CSS imported successfully.";
        return RedirectToAction("Profile");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> EquipDecoration([FromBody] DecorationRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return Json(new { success = false, message = "User not found." });

        if (request.DecorationId == 0)
        {
            user.EquippedDecorationId = null;
        }
        else
        {
            var decoration = await _context.PfpDecorations.FindAsync(request.DecorationId);
            if (decoration == null) return Json(new { success = false, message = "Decoration not found." });

            var isUnlocked = await _context.UserUnlockedDecorations
                .AnyAsync(ud => ud.UserId == userId && ud.DecorationId == request.DecorationId);
            
            bool canEquip = (user.Level >= decoration.LevelRequirement && !decoration.IsLocked) || isUnlocked;
            
            if (!canEquip) return Json(new { success = false, message = "Decoration not unlocked." });
            user.EquippedDecorationId = request.DecorationId;
        }

        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> EquipTitle([FromBody] TitleRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return Json(new { success = false, message = "User not found." });

        if (request.TitleId == 0)
        {
            user.EquippedTitleId = null;
        }
        else
        {
            var title = await _context.UserTitles.FindAsync(request.TitleId);
            if (title == null) return Json(new { success = false, message = "Title not found." });

            var isUnlocked = await _context.UserUnlockedTitles
                .AnyAsync(ut => ut.UserId == userId && ut.TitleId == request.TitleId);
            
            bool canEquip = (user.Level >= title.LevelRequirement && !title.IsLocked) || isUnlocked;

            if (!canEquip) return Json(new { success = false, message = "Title not unlocked." });
            user.EquippedTitleId = request.TitleId;
        }

        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
}

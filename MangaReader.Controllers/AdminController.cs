using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BCrypt.Net;
using HtmlAgilityPack;
using MangaReader.Data;
using MangaReader.Models;
using MangaReader.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MangaReader.Controllers;

[Authorize]
public class AdminController(ApplicationDbContext context, ILogger<AdminController> logger, IWebHostEnvironment webHostEnvironment, IChapterService chapterService) : Controller
{
	private readonly ApplicationDbContext _context = context;
	private readonly ILogger<AdminController> _logger = logger;
	private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
	private readonly IChapterService _chapterService = chapterService;

	private bool IsCurrentUserAdmin()
	{
		return base.User?.FindFirst("IsAdmin")?.Value == "True";
	}

	private bool IsCurrentUserSubAdmin()
	{
		return base.User?.FindFirst("IsSubAdmin")?.Value == "True";
	}

	public async Task<IActionResult> Index(string period = "Daily")
	{
		ViewBag.IsMaintenance = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "MaintenanceMode"))?.Value == "true";
		ViewBag.CurrentPeriod = period;
		DateTime now = DateTime.UtcNow;
		DateTime startDate;
		switch (period)
		{
		case "Weekly":
			startDate = now.Date.AddDays(-6.0);
			break;
		case "Monthly":
			startDate = now.Date.AddDays(-29.0);
			break;
		case "Yearly":
			startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
			break;
		default:
			startDate = now.AddHours(-23.0);
			break;
		}

		
		try
		{
			using var command = _context.Database.GetDbConnection().CreateCommand();
			await _context.Database.OpenConnectionAsync();
			
			command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'ChapterComments' AND COLUMN_NAME = 'ParentCommentId' AND TABLE_SCHEMA = DATABASE();";
			bool colMissing = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;
			
			command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'SiteSettings' AND TABLE_SCHEMA = DATABASE();";
			bool tableMissing = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;
			
			command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'Notifications' AND TABLE_SCHEMA = DATABASE();";
			bool notifyMissing = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;
			
			command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Mangas' AND COLUMN_NAME = 'AniListId' AND TABLE_SCHEMA = DATABASE();";
			bool aniListColMissing = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;
			
			command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'FollowChangelog' AND TABLE_SCHEMA = DATABASE();";
			bool followChangelogColMissing = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;
			
			command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'CustomPrimaryColor' AND TABLE_SCHEMA = DATABASE();";
			bool flag = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;

			
			command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'ChapterViews' AND COLUMN_NAME = 'Device' AND TABLE_SCHEMA = DATABASE();";
			bool deviceColMissing = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;

			command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'ChapterViews' AND COLUMN_NAME = 'SessionId' AND TABLE_SCHEMA = DATABASE();";
			bool sessionColMissing = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;

			command.CommandText = "SELECT IS_NULLABLE FROM information_schema.COLUMNS WHERE TABLE_NAME = 'ChapterViews' AND COLUMN_NAME = 'UserId' AND TABLE_SCHEMA = DATABASE();";
			string? isNullable = (await command.ExecuteScalarAsync())?.ToString();
			bool userIdNotNull = isNullable != null && isNullable.ToUpper() == "NO";
			
			ViewBag.DatabaseFixNeeded = colMissing || tableMissing || notifyMissing || aniListColMissing || followChangelogColMissing || flag || deviceColMissing || sessionColMissing || userIdNotNull;
			ViewBag.SiteSettingsTableMissing = tableMissing;
			ViewBag.ParentCommentIdMissing = colMissing;
			ViewBag.NotificationsTableMissing = notifyMissing;
			ViewBag.AniListIdMissing = aniListColMissing;
			ViewBag.FollowChangelogMissing = followChangelogColMissing;
			ViewBag.CustomizationColsMissing = flag;
			ViewBag.ChapterViewsColsMissing = deviceColMissing || sessionColMissing || userIdNotNull;
		}
		catch
		{
			ViewBag.DatabaseFixNeeded = false;
		}

		AdminDashboardViewModel stats = new AdminDashboardViewModel();
		try 
		{
			int totalUsers = await _context.Users.CountAsync();
			var periodViews = await _context.ChapterViews.Where(cv => cv.ViewedAt >= startDate).ToListAsync();
			int uniqueVisitors = periodViews.Select(cv => cv.UserId?.ToString() ?? cv.SessionId ?? cv.Id.ToString()).Distinct().Count();

			stats.TotalManga = await _context.Mangas.CountAsync();
			stats.TotalChapters = await _context.Chapters.CountAsync();
			stats.TotalPages = await _context.ChapterPages.CountAsync();
			stats.TotalViews = periodViews.Count;
			stats.TotalUsers = totalUsers;
			stats.UniqueVisitors = uniqueVisitors;
			stats.NewUsersLast7Days = await _context.Users.CountAsync(u => u.CreatedAt >= now.AddDays(-7.0));
			stats.ActiveRate = (totalUsers > 0) ? Math.Min(100.0, (double)periodViews.Select(cv => cv.UserId).Distinct().Count() / (double)totalUsers * 100.0) : 0.0;
			stats.UserEngagement = (totalUsers > 0) ? ((double)periodViews.Count / (double)totalUsers) : 0.0;
			stats.RecentManga = await _context.Mangas.OrderByDescending(m => m.CreatedAt).Take(5).ToListAsync();
			stats.RecentChapters = await _context.Chapters.Include(c => c.Manga).OrderByDescending(c => c.CreatedAt).Take(5).ToListAsync();
			stats.PopularChapters = await _context.Chapters.Include(c => c.Manga)
				.OrderByDescending(c => c.ViewCount)
				.Take(5)
				.Select(c => new PopularChapterViewModel
				{
					MangaTitle = (c.Manga != null) ? c.Manga.Title : "Unknown",
					ChapterNumber = (double)c.ChapterNumber,
					Views = c.ViewCount
				}).ToListAsync();

			switch (period)
			{
			case "Daily":
				for (int i = 23; i >= 0; i--)
				{
					DateTime hour = now.AddHours(-i);
					stats.TrafficLabels.Add(hour.ToString("HH:00"));
					int views = periodViews.Count(cv => cv.ViewedAt.Date == hour.Date && cv.ViewedAt.Hour == hour.Hour);
					int visitors = periodViews.Where(cv => cv.ViewedAt.Date == hour.Date && cv.ViewedAt.Hour == hour.Hour).Select(cv => cv.UserId?.ToString() ?? cv.SessionId ?? cv.Id.ToString()).Distinct().Count();
					stats.TrafficViews.Add(views);
					stats.TrafficVisitors.Add(visitors);
				}
				break;
			case "Weekly":
			case "Monthly":
				int days = (period == "Weekly") ? 7 : 30;
				for (int i = days - 1; i >= 0; i--)
				{
					DateTime date = now.Date.AddDays(-i);
					stats.TrafficLabels.Add(date.ToString("MMM dd"));
					int views = periodViews.Count(cv => cv.ViewedAt.Date == date);
					int visitors = periodViews.Where(cv => cv.ViewedAt.Date == date).Select(cv => cv.UserId?.ToString() ?? cv.SessionId ?? cv.Id.ToString()).Distinct().Count();
					stats.TrafficViews.Add(views);
					stats.TrafficVisitors.Add(visitors);
				}
				break;
			case "Yearly":
				for (int i = 11; i >= 0; i--)
				{
					DateTime month = now.Date.AddMonths(-i);
					stats.TrafficLabels.Add(month.ToString("MMM yyyy"));
					int views = periodViews.Count(cv => cv.ViewedAt.Year == month.Year && cv.ViewedAt.Month == month.Month);
					int visitors = periodViews.Where(cv => cv.ViewedAt.Year == month.Year && cv.ViewedAt.Month == month.Month).Select(cv => cv.UserId?.ToString() ?? cv.SessionId ?? cv.Id.ToString()).Distinct().Count();
					stats.TrafficViews.Add(views);
					stats.TrafficVisitors.Add(visitors);
				}
				break;
			}

			var genres = (await _context.Mangas.Select(m => m.Genre).ToListAsync())
				.SelectMany(g => (g ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
				.Select(g => g.Trim())
				.GroupBy(g => g)
				.OrderByDescending(g => g.Count())
				.Take(5);

			foreach (var genre in genres)
			{
				stats.GenreLabels.Add(genre.Key);
				stats.GenreCounts.Add(genre.Count());
			}

			var deviceStats = periodViews
				.GroupBy(cv => cv.Device ?? "Desktop")
				.Select(g => new { Device = g.Key, Count = g.Count() })
				.OrderByDescending(g => g.Count)
				.ToList();

			foreach (var ds in deviceStats)
			{
				stats.DeviceLabels.Add(ds.Device);
				stats.DeviceCounts.Add(ds.Count);
			}

			if (!stats.DeviceLabels.Any())
			{
				stats.DeviceLabels = new List<string> { "Mobile", "Desktop", "Tablet" };
				stats.DeviceCounts = new List<int> { 0, 0, 0 };
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading admin dashboard stats. Database may need fixing.");
			ViewBag.DatabaseFixNeeded = true;
			ViewBag.ChapterViewsColsMissing = true;
		}

		try
		{
			ViewBag.EnableDecorations = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "EnableDecorations"))?.Value ?? "true";
			ViewBag.EnableTitles = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "EnableTitles"))?.Value ?? "true";
			ViewBag.EnableBannerShadow = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "EnableBannerShadow"))?.Value ?? "false";
			ViewBag.BannerShadowStrength = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "BannerShadowStrength"))?.Value ?? "0.8";
			ViewBag.BannerShadowDepth = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "BannerShadowDepth"))?.Value ?? "4";
		}
		catch
		{
			ViewBag.EnableDecorations = "true";
			ViewBag.EnableTitles = "true";
			ViewBag.EnableBannerShadow = "false";
			ViewBag.BannerShadowStrength = "0.8";
			ViewBag.BannerShadowDepth = "4";
		}

		await SeedChangelogsAsync();
		await EnsureDec30ChangelogAsync();
		return View(stats);
	}

	private async Task EnsureDec30ChangelogAsync()
	{
		string title = "The Big Update: XP, Replies & Customization";
		string oldTitle = "Notifications & UI Enhancements";
		var changelogEntry = await _context.ChangelogEntries
			.FirstOrDefaultAsync(e => e.Title == oldTitle || e.Title == title);
		
		string content = "### \ud83c\udf1f Social & Engagement\r\n- **Comment XP System**: Earn XP for your activity! Longer comments earn more XP (up to 100 XP per comment). \r\n  *Note: XP is awarded for the first comment/reply per chapter. Self-replies do not count.*\r\n- **TikTok-style Replies**: New arrow indicator showing exactly who you are replying to.\r\n- **Reply Notifications**: Get notified instantly when someone replies to your comment.\r\n- **Notification Tabs**: Organized notifications into **Series**, **Community**, and **System** categories.\r\n- **Notification Management**: You can now mark individual notifications as read or delete them entirely.\r\n\r\n### \ud83c\udfa8 UI & Customization\r\n- **Banner Shadow Controls**: Full control over the featured banner description shadow (Strength & Depth) via the Admin panel.\r\n- **Mobile Optimization**: Descriptions are now capped at 100 characters on mobile to keep the UI clean.\r\n- **Social Media Cleanup**: Updated all links to our new Discord server [discord.gg/tyRD6Nn6Fr](https://discord.gg/tyRD6Nn6Fr) and removed legacy social icons.\r\n\r\n### \ud83d\udee0\ufe0f Improvements & Fixes\r\n- **Nullable Chapter Titles**: Chapter titles are now optional! You can upload chapters with just a number.\r\n- **Database Self-Healing**: Improved system stability and automatic database error resolution.\r\n- **Real-time Leveling**: Your level and XP now update dynamically in the navigation bar.";
		
		if (changelogEntry != null)
		{
			changelogEntry.Title = title;
			changelogEntry.Content = content;
			changelogEntry.CreatedAt = new DateTime(2025, 12, 30, 12, 0, 0);
			_context.Update(changelogEntry);
		}
		else
		{
			_context.ChangelogEntries.Add(new ChangelogEntry
			{
				Title = title,
				Content = content,
				CreatedAt = new DateTime(2025, 12, 30, 12, 0, 0)
			});
		}
		await _context.SaveChangesAsync();
	}

	private async Task SeedChangelogsAsync()
	{
		if (!(await _context.ChangelogEntries.AnyAsync()))
		{
			DateTime createdAt = new DateTime(2025, 12, 28, 12, 0, 0);
			var list = new List<ChangelogEntry>
			{
				new ChangelogEntry
				{
					Title = "Social Media & Contact Updates",
					Content = "### Updated Social Links\n- Updated Discord link to our new server: [discord.gg/tyRD6Nn6Fr](https://discord.gg/tyRD6Nn6Fr)\n- Removed outdated social media links (Twitter, GitHub).\n- Linked the **Contact** page directly to our Discord server for faster support.",
					CreatedAt = createdAt
				},
				new ChangelogEntry
				{
					Title = "Comment XP & TikTok Replies",
					Content = "### New Features\n- **Comment XP System**: Earn XP for every comment you post! Longer comments (excluding markdown) earn more XP.\n- **TikTok-style Replies**: See exactly who is replying to whom with a clear arrow indicator after the username.\n- **Reply Notifications**: Get notified instantly when someone replies to your comment.\n- **XP Rules**: XP is awarded for your first comment and first reply per chapter. Self-replies do not count towards XP.",
					CreatedAt = createdAt.AddMinutes(5.0)
				},
				new ChangelogEntry
				{
					Title = "Chapter Improvements",
					Content = "### Improvements\n- **Nullable Chapter Titles**: Chapter titles are now optional. You can upload chapters with just a number if they don't have a specific title.\n- **Database Stability**: Improved database self-healing logic to prevent errors during updates.",
					CreatedAt = createdAt.AddMinutes(10.0)
				},
				new ChangelogEntry
				{
					Title = "Site Changelog & Fixes",
					Content = "### New Features\n- **Public Changelog**: Added this page to keep everyone updated on the latest site changes!\n- **Fixes**: Resolved a critical error with notification loading and improved overall site performance.",
					CreatedAt = DateTime.UtcNow
				}
			};
			_context.ChangelogEntries.AddRange(list);
			await _context.SaveChangesAsync();
		}
	}

	public async Task<IActionResult> SiteSettings()
	{
		var settings = await _context.SiteSettings.ToListAsync();
		return View(settings);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> UpdateSetting(string key, string value)
	{
		if (!IsCurrentUserAdmin())
		{
			return Forbid();
		}
		try
		{
			var siteSetting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == key);
			if (siteSetting == null)
			{
				siteSetting = new SiteSetting
				{
					Key = key,
					Value = value
				};
				_context.SiteSettings.Add(siteSetting);
			}
			else
			{
				siteSetting.Value = value;
			}
			await _context.SaveChangesAsync();
			TempData["SuccessMessage"] = "Settings updated successfully!";
		}
		catch (Exception)
		{
			TempData["ErrorMessage"] = "Failed to update settings. The SiteSettings table might be missing. Please apply database fixes first.";
		}
		return RedirectToAction("Index");
	}

	public async Task<IActionResult> MangaList(string search = "")
	{
		var query = _context.Mangas.Include(m => m.Chapters).AsQueryable();
		if (!string.IsNullOrEmpty(search))
		{
			query = query.Where(m => m.Title.Contains(search) || m.Author.Contains(search));
		}
		var model = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
		ViewBag.Search = search;
		return View(model);
	}

	public IActionResult CreateManga()
	{
		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> CreateManga([Bind("Title,Description,ImageUrl,BannerUrl,Author,Artist,Status,Type,Genre,IsFeatured,BannerPositionX,BannerPositionY,HasTitleShadow,TitleShadowSize,TitleShadowOpacity,IsSuggestive")] Manga manga)
	{
		if (ModelState.IsValid)
		{
			manga.Rating = 0; 
			manga.CreatedAt = DateTime.UtcNow;
			manga.UpdatedAt = DateTime.UtcNow;
			_context.Add(manga);
			await _context.SaveChangesAsync();
			return RedirectToAction("MangaList");
		}
		return View(manga);
	}

	public async Task<IActionResult> EditManga(int id)
	{
		var manga = await _context.Mangas.FindAsync(id);
		if (manga == null)
		{
			return NotFound();
		}
		return View(manga);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> EditManga(int id, [Bind("Id,Title,Description,ImageUrl,BannerUrl,Author,Artist,Status,Type,Genre,IsFeatured,CreatedAt,BannerPositionX,BannerPositionY,HasTitleShadow,TitleShadowSize,TitleShadowOpacity,IsSuggestive")] Manga manga)
	{
		if (id != manga.Id)
		{
			return NotFound();
		}
		if (ModelState.IsValid)
		{
			try
			{
				var existingManga = await _context.Mangas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
				if (existingManga != null)
				{
					manga.Rating = existingManga.Rating; 
				}
				manga.UpdatedAt = DateTime.UtcNow;
				_context.Update(manga);
				await _context.SaveChangesAsync();
				return RedirectToAction("MangaList");
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!(await MangaExists(manga.Id)))
				{
					return NotFound();
				}
				throw;
			}
		}
		return View(manga);
	}

	public async Task<IActionResult> DeleteManga(int id)
	{
		var manga = await _context.Mangas.FindAsync(id);
		if (manga == null)
		{
			return NotFound();
		}
		return View(manga);
	}

	[HttpPost]
	[ActionName("DeleteManga")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteMangaConfirmed(int id)
	{
		var manga = await _context.Mangas.FindAsync(id);
		if (manga != null)
		{
			_context.Mangas.Remove(manga);
			await _context.SaveChangesAsync();
		}
		return RedirectToAction("MangaList");
	}

	private async Task<bool> MangaExists(int id)
	{
		return await _context.Mangas.AnyAsync(e => e.Id == id);
	}

	public async Task<IActionResult> ChapterList(int mangaId, string search = "", string sort = "number_desc")
	{
		var manga = await _context.Mangas.FindAsync(mangaId);
		if (manga == null)
		{
			return NotFound();
		}
		var query = _context.Chapters
			.Include(c => c.Pages)
			.Include(c => c.Comments)
			.Where(c => c.MangaId == mangaId).AsQueryable();

		if (!string.IsNullOrEmpty(search))
		{
			query = query.Where(c => (c.Title != null && c.Title.Contains(search)) || c.ChapterNumber.ToString().Contains(search));
		}

		query = sort switch
		{
			"number_asc" => query.OrderBy(c => c.ChapterNumber),
			"number_desc" => query.OrderByDescending(c => c.ChapterNumber),
			"title_asc" => query.OrderBy(c => c.Title),
			"title_desc" => query.OrderByDescending(c => c.Title),
			"date_asc" => query.OrderBy(c => c.ReleasedDate),
			"date_desc" => query.OrderByDescending(c => c.ReleasedDate),
			"views_asc" => query.OrderBy(c => c.ViewCount),
			"views_desc" => query.OrderByDescending(c => c.ViewCount),
			"pages_asc" => query.OrderBy(c => c.Pages.Count),
			"pages_desc" => query.OrderByDescending(c => c.Pages.Count),
			"comments_asc" => query.OrderBy(c => c.Comments.Count),
			"comments_desc" => query.OrderByDescending(c => c.Comments.Count),
			_ => query.OrderByDescending(c => c.ChapterNumber)
		};

		var model = await query.ToListAsync();
		ViewBag.Manga = manga;
		ViewBag.Search = search;
		ViewBag.MangaId = mangaId;
		ViewBag.CurrentSort = sort;
		return View(model);
	}

	public async Task<IActionResult> CreateChapter(int mangaId)
	{
		var manga = await _context.Mangas.FindAsync(mangaId);
		if (manga == null)
		{
			return NotFound();
		}
		var model = new Chapter
		{
			MangaId = mangaId,
			Title = null,
			Description = string.Empty,
			CoverImageUrl = string.Empty,
			Manga = manga
		};
		ViewBag.Manga = manga;
		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> CreateChapter([Bind("MangaId,ChapterNumber,Title,Description,CoverImageUrl,ReleasedDate")] Chapter chapter, List<IFormFile> pages, string pageUrls)
	{
		if (ModelState.IsValid)
		{
			try
			{
				await _chapterService.CreateChapterAsync(chapter, pages, pageUrls);
				return RedirectToAction("ChapterList", new
				{
					mangaId = chapter.MangaId
				});
			}
			catch (Exception)
			{
				ModelState.AddModelError("", "An error occurred while creating the chapter.");
			}
		}
		var manga = await _context.Mangas.FindAsync(chapter.MangaId);
		ViewBag.Manga = manga;
		ViewBag.PageUrls = pageUrls;
		return View(chapter);
	}

	public async Task<IActionResult> EditChapter(int id)
	{
		var chapter = await _context.Chapters.Include(c => c.Manga).FirstOrDefaultAsync(c => c.Id == id);
		if (chapter == null)
		{
			return NotFound();
		}
		ViewBag.Manga = chapter.Manga;
		return View(chapter);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> EditChapter(int id, [Bind("Id,MangaId,ChapterNumber,Title,Description,CoverImageUrl,ReleasedDate,ViewCount,CreatedAt")] Chapter chapter)
	{
		if (id != chapter.Id)
		{
			return NotFound();
		}
		if (ModelState.IsValid)
		{
			try
			{
				chapter.UpdatedAt = DateTime.UtcNow;
				_context.Update(chapter);
				var manga = await _context.Mangas.FindAsync(chapter.MangaId);
				if (manga != null)
				{
					manga.UpdatedAt = DateTime.UtcNow;
				}
				await _context.SaveChangesAsync();
				return RedirectToAction("ChapterList", new
				{
					mangaId = chapter.MangaId
				});
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!(await ChapterExists(chapter.Id)))
				{
					return NotFound();
				}
				throw;
			}
		}
		var manga2 = await _context.Mangas.FindAsync(chapter.MangaId);
		ViewBag.Manga = manga2;
		return View(chapter);
	}

	public async Task<IActionResult> DeleteChapter(int id)
	{
		var chapter = await _context.Chapters.Include(c => c.Manga).FirstOrDefaultAsync(c => c.Id == id);
		if (chapter == null)
		{
			return NotFound();
		}
		return View(chapter);
	}

	[HttpPost]
	[ActionName("DeleteChapter")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteChapterConfirmed(int id)
	{
		var chapter = await _context.Chapters.FindAsync(id);
		if (chapter != null)
		{
			int mangaId = chapter.MangaId;
			_context.Chapters.Remove(chapter);
			await _context.SaveChangesAsync();
			return RedirectToAction("ChapterList", new { mangaId });
		}
		return NotFound();
	}

	private async Task<bool> ChapterExists(int id)
	{
		return await _context.Chapters.AnyAsync(e => e.Id == id);
	}

	public async Task<IActionResult> PageList(int chapterId)
	{
		var chapter = await _context.Chapters
			.Include(c => c.Manga)
			.Include(c => c.Pages)
			.FirstOrDefaultAsync(c => c.Id == chapterId);
		if (chapter == null)
		{
			return NotFound();
		}
		var model = await _context.ChapterPages
			.Where(p => p.ChapterId == chapterId)
			.OrderBy(p => p.PageNumber)
			.ToListAsync();
		ViewBag.Chapter = chapter;
		ViewBag.ChapterId = chapterId;
		return View(model);
	}

	public async Task<IActionResult> CreatePage(int chapterId)
	{
		var chapter = await _context.Chapters
			.Include(c => c.Manga)
			.FirstOrDefaultAsync(c => c.Id == chapterId);
		if (chapter == null)
		{
			return NotFound();
		}
		int num = await _context.ChapterPages.CountAsync(p => p.ChapterId == chapterId);
		var model = new ChapterPage
		{
			ChapterId = chapterId,
			PageNumber = num + 1,
			ImageUrl = string.Empty,
			Chapter = chapter
		};
		ViewBag.Chapter = chapter;
		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> CreatePage([Bind("ChapterId,PageNumber,ImageUrl,Width,Height")] ChapterPage page)
	{
		if (ModelState.IsValid)
		{
			page.CreatedAt = DateTime.UtcNow;
			_context.Add(page);
			await _context.SaveChangesAsync();
			return RedirectToAction("PageList", new
			{
				chapterId = page.ChapterId
			});
		}
		var chapter = await _context.Chapters
			.Include(c => c.Manga)
			.FirstOrDefaultAsync(c => c.Id == page.ChapterId);
		ViewBag.Chapter = chapter;
		return View(page);
	}

	public async Task<IActionResult> EditPage(int id)
	{
		var chapterPage = await _context.ChapterPages
			.Include(p => p.Chapter!)
				.ThenInclude(c => c.Manga)
			.FirstOrDefaultAsync(p => p.Id == id);
		if (chapterPage == null)
		{
			return NotFound();
		}
		ViewBag.Chapter = chapterPage.Chapter;
		return View(chapterPage);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> EditPage(int id, [Bind("Id,ChapterId,PageNumber,ImageUrl,Width,Height,CreatedAt")] ChapterPage page)
	{
		if (id != page.Id)
		{
			return NotFound();
		}
		if (ModelState.IsValid)
		{
			try
			{
				_context.Update(page);
				await _context.SaveChangesAsync();
				return RedirectToAction("PageList", new
				{
					chapterId = page.ChapterId
				});
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!(await PageExists(page.Id)))
				{
					return NotFound();
				}
				throw;
			}
		}
		var chapter = await _context.Chapters
			.Include(c => c.Manga)
			.FirstOrDefaultAsync(c => c.Id == page.ChapterId);
		ViewBag.Chapter = chapter;
		return View(page);
	}

	public async Task<IActionResult> DeletePage(int id)
	{
		var chapterPage = await _context.ChapterPages
			.Include(p => p.Chapter)
			.FirstOrDefaultAsync(p => p.Id == id);
		if (chapterPage == null)
		{
			return NotFound();
		}
		return View(chapterPage);
	}

	[HttpPost]
	[ActionName("DeletePage")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeletePageConfirmed(int id)
	{
		var chapterPage = await _context.ChapterPages.FindAsync(id);
		if (chapterPage != null)
		{
			int chapterId = chapterPage.ChapterId;
			_context.ChapterPages.Remove(chapterPage);
			await _context.SaveChangesAsync();
			return RedirectToAction("PageList", new { chapterId });
		}
		return NotFound();
	}

	public async Task<IActionResult> ChangelogList()
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Forbid();
		}
		return View(await _context.ChangelogEntries.OrderByDescending(e => e.CreatedAt).ToListAsync());
	}

	public IActionResult CreateChangelog()
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Forbid();
		}
		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> CreateChangelog([Bind("Title,Content,CreatedAt")] ChangelogEntry entry, [FromServices] INotificationService notificationService)
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Forbid();
		}
		if (ModelState.IsValid)
		{
			if (entry.CreatedAt == default(DateTime))
			{
				entry.CreatedAt = DateTime.UtcNow;
			}
			_context.Add(entry);
			await _context.SaveChangesAsync();
			
			var usersToNotify = await _context.Users.Where(u => u.FollowChangelog).ToListAsync();
			foreach (var user in usersToNotify)
			{
				await notificationService.CreateNotificationAsync(user.Id, NotificationType.System, "New Update: " + entry.Title);
			}
			return RedirectToAction("ChangelogList");
		}
		return View(entry);
	}

	public async Task<IActionResult> EditChangelog(int id)
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Forbid();
		}
		var changelogEntry = await _context.ChangelogEntries.FindAsync(id);
		if (changelogEntry == null)
		{
			return NotFound();
		}
		return View(changelogEntry);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> EditChangelog(int id, [Bind("Id,Title,Content,CreatedAt")] ChangelogEntry entry)
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Forbid();
		}
		if (id != entry.Id)
		{
			return NotFound();
		}
		if (ModelState.IsValid)
		{
			_context.Update(entry);
			await _context.SaveChangesAsync();
			return RedirectToAction("ChangelogList");
		}
		return View(entry);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteChangelog(int id)
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Forbid();
		}
		var changelogEntry = await _context.ChangelogEntries.FindAsync(id);
		if (changelogEntry != null)
		{
			_context.ChangelogEntries.Remove(changelogEntry);
			await _context.SaveChangesAsync();
		}
		return RedirectToAction("ChangelogList");
	}

	private async Task<bool> PageExists(int id)
	{
		return await _context.ChapterPages.AnyAsync(e => e.Id == id);
	}

	public async Task<IActionResult> BulkCreatePages(int chapterId)
	{
		var chapter = await _context.Chapters
			.Include(c => c.Manga)
			.FirstOrDefaultAsync(c => c.Id == chapterId);
		if (chapter == null)
		{
			return NotFound();
		}
		ViewBag.Chapter = chapter;
		ViewBag.ChapterId = chapterId;
		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> BulkCreatePages(int chapterId, string imageUrls)
	{
		var chapter = await _context.Chapters
			.Include(c => c.Manga)
			.FirstOrDefaultAsync(c => c.Id == chapterId);
		if (chapter == null)
		{
			return NotFound();
		}
		if (string.IsNullOrWhiteSpace(imageUrls))
		{
			ModelState.AddModelError("", "Please provide at least one image URL.");
			ViewBag.Chapter = chapter;
			ViewBag.ChapterId = chapterId;
			return View();
		}
		int num = await _context.ChapterPages.CountAsync(p => p.ChapterId == chapterId);
		var list = imageUrls.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(u => u.Trim())
			.Where(u => !string.IsNullOrWhiteSpace(u))
			.ToList();
		if (list.Count == 0)
		{
			ModelState.AddModelError("", "No valid URLs found.");
			ViewBag.Chapter = chapter;
			ViewBag.ChapterId = chapterId;
			return View();
		}
		var pages = new List<ChapterPage>();
		for (int i = 0; i < list.Count; i++)
		{
			pages.Add(new ChapterPage
			{
				ChapterId = chapterId,
				PageNumber = num + i + 1,
				ImageUrl = list[i],
				CreatedAt = DateTime.UtcNow
			});
		}
		_context.ChapterPages.AddRange(pages);
		await _context.SaveChangesAsync();
		TempData["SuccessMessage"] = $"Successfully added {pages.Count} pages!";
		return RedirectToAction("PageList", new { chapterId });
	}

	public async Task<IActionResult> ManageUsers(string search = "")
	{
		var query = _context.Users.AsQueryable();
		if (!string.IsNullOrWhiteSpace(search))
		{
			query = query.Where(u => u.Username.Contains(search));
		}
		var users = await query.OrderBy(u => u.Username).ToListAsync();
		ViewBag.Search = search;
		ViewBag.CanManage = IsCurrentUserAdmin();
		ViewBag.AvailableTitles = await _context.UserTitles.OrderBy(t => t.Name).ToListAsync();
		ViewBag.AvailableDecorations = await _context.PfpDecorations.OrderBy(d => d.Name).ToListAsync();
		return View(users);
	}

	public async Task<IActionResult> ManageContent(string search = "", string sort = "date_desc")
	{
		var query = _context.Chapters
			.Include(c => c.Manga)
			.Include(c => c.Comments)
			.AsQueryable();

		if (!string.IsNullOrEmpty(search))
		{
			query = query.Where(c => (c.Manga != null && c.Manga.Title.Contains(search)) || 
			                         (c.Title != null && c.Title.Contains(search)) || 
			                         c.ChapterNumber.ToString().Contains(search));
		}

		query = sort switch
		{
			"manga_asc" => query.OrderBy(c => c.Manga != null ? c.Manga.Title : ""),
			"manga_desc" => query.OrderByDescending(c => c.Manga != null ? c.Manga.Title : ""),
			"number_asc" => query.OrderBy(c => c.ChapterNumber),
			"number_desc" => query.OrderByDescending(c => c.ChapterNumber),
			"views_asc" => query.OrderBy(c => c.ViewCount),
			"views_desc" => query.OrderByDescending(c => c.ViewCount),
			"comments_asc" => query.OrderBy(c => c.Comments.Count),
			"comments_desc" => query.OrderByDescending(c => c.Comments.Count),
			"date_asc" => query.OrderBy(c => c.CreatedAt),
			"date_desc" => query.OrderByDescending(c => c.CreatedAt),
			_ => query.OrderByDescending(c => c.CreatedAt)
		};

		var model = await query.Take(100).ToListAsync(); 
		ViewBag.Search = search;
		ViewBag.CurrentSort = sort;
		return View(model);
	}

	public async Task<IActionResult> ManageComments(int? chapterId, int? userId, string search = "")
	{
		var query = _context.ChapterComments
			.Include(c => c.User)
			.Include(c => c.Chapter)
				.ThenInclude(ch => ch.Manga)
			.AsQueryable();

		if (chapterId.HasValue)
		{
			query = query.Where(c => c.ChapterId == chapterId.Value);
			ViewBag.Chapter = await _context.Chapters.Include(ch => ch.Manga).FirstOrDefaultAsync(ch => ch.Id == chapterId.Value);
		}

		if (userId.HasValue)
		{
			query = query.Where(c => c.UserId == userId.Value);
			ViewBag.User = await _context.Users.FindAsync(userId.Value);
		}

		if (!string.IsNullOrWhiteSpace(search))
		{
			query = query.Where(c => c.Content.Contains(search) || (c.User != null && c.User.Username.Contains(search)));
		}

		var comments = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
		ViewBag.Search = search;
		ViewBag.ChapterId = chapterId;
		ViewBag.UserId = userId;

		return View(comments);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteComment(int id)
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Forbid();
		}

		var comment = await _context.ChapterComments.FindAsync(id);
		if (comment == null)
		{
			return NotFound();
		}

		_context.ChapterComments.Remove(comment);
		await _context.SaveChangesAsync();

		TempData["SuccessMessage"] = "Comment deleted successfully.";
		return RedirectToAction("ManageComments", new { chapterId = comment.ChapterId });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> CreateUser(string username, string password, bool isAdmin = false, bool isSubAdmin = false, bool isActive = true)
	{
		if (!IsCurrentUserAdmin())
		{
			return Forbid();
		}
		if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
		{
			TempData["ErrorMessage"] = "Username and password are required.";
			return RedirectToAction("ManageUsers");
		}
		if (await _context.Users.AnyAsync(u => u.Username == username))
		{
			TempData["ErrorMessage"] = "Username already exists.";
			return RedirectToAction("ManageUsers");
		}
		User user = new User
		{
			Username = username,
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
			IsAdmin = isAdmin,
			IsSubAdmin = isSubAdmin,
			IsActive = isActive,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		_context.Users.Add(user);
		await _context.SaveChangesAsync();
		TempData["SuccessMessage"] = "User created successfully.";
		return RedirectToAction("ManageUsers");
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> UpdateRole(int userId, bool isAdmin, bool isSubAdmin)
	{
		if (!IsCurrentUserAdmin())
		{
			return Forbid();
		}
		User? user = await _context.Users.FindAsync(userId);
		if (user == null)
		{
			TempData["ErrorMessage"] = "User not found.";
			return RedirectToAction("ManageUsers");
		}
		user.IsAdmin = isAdmin;
		user.IsSubAdmin = isSubAdmin;
		user.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();
		TempData["SuccessMessage"] = "Roles updated.";
		return RedirectToAction("ManageUsers");
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ResetUserPassword(int userId, string newPassword)
	{
		if (!IsCurrentUserAdmin())
		{
			return Forbid();
		}
		if (string.IsNullOrWhiteSpace(newPassword))
		{
			TempData["ErrorMessage"] = "New password cannot be empty.";
			return RedirectToAction("ManageUsers");
		}
		User? user = await _context.Users.FindAsync(userId);
		if (user == null)
		{
			TempData["ErrorMessage"] = "User not found.";
			return RedirectToAction("ManageUsers");
		}
		user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
		user.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();
		TempData["SuccessMessage"] = "Password for user '" + user.Username + "' has been reset successfully.";
		return RedirectToAction("ManageUsers");
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ToggleActive(int userId)
	{
		if (!IsCurrentUserAdmin())
		{
			return Forbid();
		}
		User? user = await _context.Users.FindAsync(userId);
		if (user == null)
		{
			TempData["ErrorMessage"] = "User not found.";
			return RedirectToAction("ManageUsers");
		}
		user.IsActive = !user.IsActive;
		user.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();
		TempData["SuccessMessage"] = "User status updated.";
		return RedirectToAction("ManageUsers");
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> AwardTitle(int userId, int titleId)
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Forbid();
		}
		User? user = await _context.Users.FindAsync(userId);
		UserTitle? title = await _context.UserTitles.FindAsync(titleId);
		if (user == null || title == null)
		{
			TempData["ErrorMessage"] = "User or Title not found.";
			return RedirectToAction("ManageUsers");
		}
		if (!(await _context.Set<UserUnlockedTitle>().AnyAsync(ut => ut.UserId == userId && ut.TitleId == titleId)))
		{
			_context.Set<UserUnlockedTitle>().Add(new UserUnlockedTitle
			{
				UserId = userId,
				TitleId = titleId,
				UnlockedAt = DateTime.UtcNow,
				Origin = UnlockOrigin.AdminAward
			});
			await _context.SaveChangesAsync();
			TempData["Success"] = $"Title '{title.Name}' awarded to {user.Username}.";
		}
		else
		{
			TempData["Error"] = user.Username + " already has the title '" + title.Name + "'.";
		}
		return RedirectToAction("ManageUsers");
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> AwardDecoration(int userId, int decorationId)
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Forbid();
		}
		User? user = await _context.Users.FindAsync(userId);
		PfpDecoration? decoration = await _context.PfpDecorations.FindAsync(decorationId);
		if (user == null || decoration == null)
		{
			TempData["Error"] = "User or Decoration not found.";
			return RedirectToAction("ManageUsers");
		}
		if (!(await _context.Set<UserUnlockedDecoration>().AnyAsync(ud => ud.UserId == userId && ud.DecorationId == decorationId)))
		{
			_context.Set<UserUnlockedDecoration>().Add(new UserUnlockedDecoration
			{
				UserId = userId,
				DecorationId = decorationId,
				UnlockedAt = DateTime.UtcNow,
				Origin = UnlockOrigin.AdminAward
			});
			await _context.SaveChangesAsync();
			TempData["Success"] = $"Decoration '{decoration.Name}' awarded to {user.Username}.";
		}
		else
		{
			TempData["Error"] = user.Username + " already has the decoration '" + decoration.Name + "'.";
		}
		return RedirectToAction("ManageUsers");
	}

	public IActionResult ManageEmojis()
	{
		string path = Path.Combine(_webHostEnvironment.WebRootPath, "emojis.json");
		string json = System.IO.File.ReadAllText(path);
		List<EmojiModel> model = JsonSerializer.Deserialize<List<EmojiModel>>(json) ?? new List<EmojiModel>();
		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public IActionResult ManageEmojis(string emojisJson)
	{
		if (string.IsNullOrWhiteSpace(emojisJson))
		{
			return BadRequest("JSON cannot be empty");
		}
		try
		{
			List<EmojiModel>? list = JsonSerializer.Deserialize<List<EmojiModel>>(emojisJson);
			if (list == null)
			{
				return BadRequest("Invalid JSON format");
			}
			string path = Path.Combine(_webHostEnvironment.WebRootPath, "emojis.json");
			System.IO.File.WriteAllText(path, emojisJson);
			TempData["SuccessMessage"] = "Emojis updated successfully!";
		}
		catch (Exception ex)
		{
			TempData["ErrorMessage"] = "Error updating emojis: " + ex.Message;
		}
		return RedirectToAction("ManageEmojis");
	}

	public async Task<IActionResult> ManageDecorations()
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Unauthorized();
		}
		return View(await _context.PfpDecorations.OrderByDescending(d => d.CreatedAt).ToListAsync());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> CreateDecoration(PfpDecoration decoration)
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Unauthorized();
		}
		if (ModelState.IsValid)
		{
			decoration.CreatedAt = DateTime.UtcNow;
			_context.PfpDecorations.Add(decoration);
			await _context.SaveChangesAsync();
			return RedirectToAction("ManageDecorations");
		}
		return View("ManageDecorations", await _context.PfpDecorations.ToListAsync());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> EditDecoration(PfpDecoration decoration)
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Unauthorized();
		}
		if (ModelState.IsValid)
		{
			PfpDecoration? pfpDecoration = await _context.PfpDecorations.FindAsync(decoration.Id);
			if (pfpDecoration == null)
			{
				return NotFound();
			}
			pfpDecoration.Name = decoration.Name;
			pfpDecoration.ImageUrl = decoration.ImageUrl;
			pfpDecoration.LevelRequirement = decoration.LevelRequirement;
			pfpDecoration.IsAnimated = decoration.IsAnimated;
			pfpDecoration.IsLocked = decoration.IsLocked;
			_context.Update(pfpDecoration);
			await _context.SaveChangesAsync();
			return RedirectToAction("ManageDecorations");
		}
		return View("ManageDecorations", await _context.PfpDecorations.ToListAsync());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteDecoration(int id)
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Unauthorized();
		}
		PfpDecoration? pfpDecoration = await _context.PfpDecorations.FindAsync(id);
		if (pfpDecoration != null)
		{
			_context.PfpDecorations.Remove(pfpDecoration);
			await _context.SaveChangesAsync();
		}
		return RedirectToAction("ManageDecorations");
	}

	public async Task<IActionResult> ManageTitles()
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Unauthorized();
		}
		return View(await _context.UserTitles.OrderByDescending(t => t.CreatedAt).ToListAsync());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> CreateTitle(UserTitle title)
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Unauthorized();
		}
		if (ModelState.IsValid)
		{
			title.CreatedAt = DateTime.UtcNow;
			_context.UserTitles.Add(title);
			await _context.SaveChangesAsync();
			return RedirectToAction("ManageTitles");
		}
		return View("ManageTitles", await _context.UserTitles.ToListAsync());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> EditTitle(UserTitle title)
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Unauthorized();
		}
		if (ModelState.IsValid)
		{
			UserTitle? userTitle = await _context.UserTitles.FindAsync(title.Id);
			if (userTitle == null)
			{
				return NotFound();
			}
			userTitle.Name = title.Name;
			userTitle.Color = title.Color;
			userTitle.LevelRequirement = title.LevelRequirement;
			userTitle.IsLocked = title.IsLocked;
			_context.Update(userTitle);
			await _context.SaveChangesAsync();
			return RedirectToAction("ManageTitles");
		}
		return View("ManageTitles", await _context.UserTitles.ToListAsync());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteTitle(int id)
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return Unauthorized();
		}
		UserTitle? userTitle = await _context.UserTitles.FindAsync(id);
		if (userTitle != null)
		{
			_context.UserTitles.Remove(userTitle);
			await _context.SaveChangesAsync();
		}
		return RedirectToAction("ManageTitles");
	}

	public async Task<IActionResult> BloggerImport()
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return RedirectToAction("Index", "Home");
		}
		ViewBag.Mangas = await _context.Mangas.OrderBy(m => m.Title).ToListAsync();
		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> BloggerImport(string bloggerUrl, int mangaId, string seriesTag, string chapterTag = "chapter")
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return RedirectToAction("Index", "Home");
		}
		if (string.IsNullOrEmpty(bloggerUrl))
		{
			TempData["ErrorMessage"] = "Blogger URL is required.";
			return RedirectToAction("BloggerImport");
		}
		if (string.IsNullOrEmpty(seriesTag))
		{
			TempData["ErrorMessage"] = "Series Tag is required to filter chapters correctly.";
			return RedirectToAction("BloggerImport");
		}
		try
		{
			string text = bloggerUrl.Trim().TrimEnd('/');
			if (!text.StartsWith("http://") && !text.StartsWith("https://"))
			{
				text = "https://" + text;
			}
			string feedUrl = text + "/feeds/posts/default/-/" + Uri.EscapeDataString(chapterTag) + "?alt=json&max-results=500";
			using HttpClient client = new HttpClient();
			client.DefaultRequestHeaders.Add("User-Agent", "DFlowScans-BloggerImporter");
			HttpResponseMessage httpResponseMessage = await client.GetAsync(feedUrl);
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				TempData["ErrorMessage"] = $"Failed to fetch Blogger feed. Status: {httpResponseMessage.StatusCode}. URL: {feedUrl}";
				return RedirectToAction("BloggerImport");
			}
			using JsonDocument doc = JsonDocument.Parse(await httpResponseMessage.Content.ReadAsStringAsync());
			if (!doc.RootElement.TryGetProperty("feed", out var value) || !value.TryGetProperty("entry", out var value2))
			{
				TempData["ErrorMessage"] = "No entries found in the Blogger feed with the specified tag.";
				return RedirectToAction("BloggerImport");
			}
			int importedChapters = 0;
			foreach (JsonElement item in value2.EnumerateArray())
			{
				bool flag = false;
				if (item.TryGetProperty("category", out var value3))
				{
					foreach (JsonElement item2 in value3.EnumerateArray())
					{
						if (item2.TryGetProperty("term", out var value4) && value4.GetString() == seriesTag)
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					continue;
				}
				string title = "Unknown Chapter";
				if (item.TryGetProperty("title", out var value5))
				{
					title = value5.GetProperty("$t").GetString() ?? "Unknown Chapter";
				}
				string html = "";
				if (item.TryGetProperty("content", out var value6))
				{
					html = value6.GetProperty("$t").GetString() ?? "";
				}
				List<string> imageUrls = ExtractImageUrls(html);
				if (imageUrls.Count != 0)
				{
					decimal chapterNumber = ParseChapterNumber(title);
					Chapter chapter = new Chapter
					{
						MangaId = mangaId,
						ChapterNumber = chapterNumber,
						Title = title,
						CreatedAt = DateTime.UtcNow,
						Description = string.Empty,
						CoverImageUrl = (imageUrls.FirstOrDefault() ?? string.Empty)
					};
					_context.Chapters.Add(chapter);
					await _context.SaveChangesAsync();
					List<ChapterPage> list = new List<ChapterPage>();
					for (int i = 0; i < imageUrls.Count; i++)
					{
						list.Add(new ChapterPage
						{
							ChapterId = chapter.Id,
							ImageUrl = imageUrls[i],
							PageNumber = i + 1,
							CreatedAt = DateTime.UtcNow
						});
					}
					_context.ChapterPages.AddRange(list);
					await _context.SaveChangesAsync();
					importedChapters++;
				}
			}
			TempData["SuccessMessage"] = $"Successfully imported {importedChapters} chapters from Blogger!";
			return RedirectToAction("MangaList");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error importing from Blogger");
			TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
			return RedirectToAction("BloggerImport");
		}
	}

	public IActionResult BloggerSeriesImport()
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return RedirectToAction("Index", "Home");
		}
		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> BloggerSeriesImport(string htmlContent, string tags)
	{
		if (!IsCurrentUserAdmin() && !IsCurrentUserSubAdmin())
		{
			return RedirectToAction("Index", "Home");
		}
		if (string.IsNullOrEmpty(htmlContent) || string.IsNullOrEmpty(tags))
		{
			TempData["ErrorMessage"] = "HTML content and tags are required.";
			return RedirectToAction("BloggerSeriesImport");
		}
		try
		{
			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(htmlContent);
			HtmlNode labelNode = doc.DocumentNode.SelectSingleNode("//div[@class='chapter_get']");
			string seriesTitle = labelNode?.GetAttributeValue("data-labelchapter", "") ?? "";
			HtmlNode synNode = doc.DocumentNode.SelectSingleNode("//p[@id='syn_bod']");
			string description = synNode?.InnerText.Trim() ?? "";
			HtmlNode imgNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'separator')]//img") 
			                   ?? doc.DocumentNode.SelectSingleNode("//img");
			string imageUrl = imgNode?.GetAttributeValue("src", "") ?? "";
			
			List<string> tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
				.Select(t => t.Trim()).ToList();
			
			string type = "Manga";
			if (tagList.Any(t => t.Equals("Manhua", StringComparison.OrdinalIgnoreCase)))
			{
				type = "Manhua";
			}
			else if (tagList.Any(t => t.Equals("Manhwa", StringComparison.OrdinalIgnoreCase)))
			{
				type = "Manhwa";
			}
			
			string status = "Ongoing";
			if (tagList.Any(t => t.Equals("Dropped", StringComparison.OrdinalIgnoreCase)))
			{
				status = "Dropped";
			}
			else if (tagList.Any(t => t.Equals("Completed", StringComparison.OrdinalIgnoreCase)))
			{
				status = "Completed";
			}
			else if (tagList.Any(t => t.Equals("Hiatus", StringComparison.OrdinalIgnoreCase)))
			{
				status = "Hiatus";
			}
			
			string[] excludedTags = { "Series", seriesTitle, type, status };
			List<string> genres = tagList.Where(t => !excludedTags.Any(e => e.Equals(t, StringComparison.OrdinalIgnoreCase))).ToList();
			string genre = string.Join(", ", genres);
			
			bool isSuggestive = tagList.Any(t => t.Contains("Suggestive", StringComparison.OrdinalIgnoreCase) || t.Contains("Ecchi", StringComparison.OrdinalIgnoreCase));
			
			Manga manga = new Manga
			{
				Title = string.IsNullOrEmpty(seriesTitle) ? "New Blogger Series" : seriesTitle,
				Description = description,
				ImageUrl = imageUrl,
				BannerUrl = imageUrl,
				Author = "Unknown",
				Artist = "Unknown",
				Status = status,
				Type = type,
				Genre = genre,
				IsFeatured = false,
				IsSuggestive = isSuggestive,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				BannerPositionX = 50,
				BannerPositionY = 50,
				HasTitleShadow = true,
				TitleShadowSize = 12,
				TitleShadowOpacity = 0.8
			};
			
			_context.Mangas.Add(manga);
			await _context.SaveChangesAsync();
			
			TempData["SuccessMessage"] = "Successfully imported series: " + manga.Title;
			return RedirectToAction("MangaList");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error importing series from Blogger HTML");
			TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
			return RedirectToAction("BloggerSeriesImport");
		}
	}

	private List<string> ExtractImageUrls(string html)
	{
		List<string> list = new List<string>();
		Regex regex = new Regex("<img[^>]+src\\s*=\\s*[\"']([^\"']+)[\"'][^>]*>", RegexOptions.IgnoreCase);
		MatchCollection matchCollection = regex.Matches(html);
		foreach (Match item in matchCollection)
		{
			list.Add(item.Groups[1].Value);
		}
		return list;
	}

	private decimal ParseChapterNumber(string title)
	{
		Match match = Regex.Match(title, "(?:Chapter\\s*)?(\\d+(?:\\.\\d+)?)", RegexOptions.IgnoreCase);
		if (match.Success && decimal.TryParse(match.Groups[1].Value, out var result))
		{
			return result;
		}
		return 0m;
	}

	public async Task<IActionResult> ManageDatabase()
	{
		if (!IsCurrentUserAdmin()) return Forbid();

		try
		{
			using var command = _context.Database.GetDbConnection().CreateCommand();
			await _context.Database.OpenConnectionAsync();

			command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'ChapterComments' AND COLUMN_NAME = 'ParentCommentId' AND TABLE_SCHEMA = DATABASE();";
			bool colMissing = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;

			command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'SiteSettings' AND TABLE_SCHEMA = DATABASE();";
			bool tableMissing = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;

			command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'Notifications' AND TABLE_SCHEMA = DATABASE();";
			bool notifyMissing = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;

			command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Mangas' AND COLUMN_NAME = 'AniListId' AND TABLE_SCHEMA = DATABASE();";
			bool aniListColMissing = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;

			command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'FollowChangelog' AND TABLE_SCHEMA = DATABASE();";
			bool followChangelogColMissing = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;

			command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'CustomPrimaryColor' AND TABLE_SCHEMA = DATABASE();";
			bool flag = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;

			command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'ChapterViews' AND COLUMN_NAME = 'Device' AND TABLE_SCHEMA = DATABASE();";
			bool deviceColMissing = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;

			command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'ChapterViews' AND COLUMN_NAME = 'SessionId' AND TABLE_SCHEMA = DATABASE();";
			bool sessionColMissing = Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;

			command.CommandText = "SELECT IS_NULLABLE FROM information_schema.COLUMNS WHERE TABLE_NAME = 'ChapterViews' AND COLUMN_NAME = 'UserId' AND TABLE_SCHEMA = DATABASE();";
			string? isNullable = (await command.ExecuteScalarAsync())?.ToString();
			bool userIdNotNull = isNullable != null && isNullable.ToUpper() == "NO";

			ViewBag.DatabaseFixNeeded = colMissing || tableMissing || notifyMissing || aniListColMissing || followChangelogColMissing || flag || deviceColMissing || sessionColMissing || userIdNotNull;
			ViewBag.SiteSettingsTableMissing = tableMissing;
			ViewBag.ParentCommentIdMissing = colMissing;
			ViewBag.NotificationsTableMissing = notifyMissing;
			ViewBag.AniListIdMissing = aniListColMissing;
			ViewBag.FollowChangelogMissing = followChangelogColMissing;
			ViewBag.CustomizationColsMissing = flag;
			ViewBag.ChapterViewsColsMissing = deviceColMissing || sessionColMissing || userIdNotNull;
		}
		catch
		{
			ViewBag.DatabaseFixNeeded = true;
		}

		ViewBag.IsMaintenance = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "MaintenanceMode"))?.Value == "true";
		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ExportDatabase()
	{
		if (!IsCurrentUserAdmin()) return Forbid();

		try
		{
			var data = new Dictionary<string, object>();
			data["Mangas"] = await _context.Mangas.AsNoTracking().ToListAsync();
			data["Chapters"] = await _context.Chapters.AsNoTracking().ToListAsync();
			data["ChapterPages"] = await _context.ChapterPages.AsNoTracking().ToListAsync();
			data["Users"] = await _context.Users.AsNoTracking().ToListAsync();
			data["PfpDecorations"] = await _context.PfpDecorations.AsNoTracking().ToListAsync();
			data["UserTitles"] = await _context.UserTitles.AsNoTracking().ToListAsync();
			data["SiteSettings"] = await _context.SiteSettings.AsNoTracking().ToListAsync();

			var options = new JsonSerializerOptions 
			{ 
				WriteIndented = true,
				ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
				DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
			};

			var json = JsonSerializer.Serialize(data, options);
			var bytes = System.Text.Encoding.UTF8.GetBytes(json);
			return File(bytes, "application/json", $"db_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to export database backup.");
			TempData["ErrorMessage"] = "Export failed: " + ex.Message;
			return RedirectToAction("ManageDatabase");
		}
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ImportDatabase(IFormFile jsonFile)
	{
		if (!IsCurrentUserAdmin()) return Forbid();
		if (jsonFile == null || jsonFile.Length == 0)
		{
			TempData["ErrorMessage"] = "Please select a JSON file to import.";
			return RedirectToAction("ManageDatabase");
		}

		try
		{
			using var stream = jsonFile.OpenReadStream();
			var data = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(stream);
			if (data == null) throw new Exception("Invalid JSON file.");

			// Mangas
			if (data.TryGetValue("Mangas", out var mangasJson))
			{
				var items = JsonSerializer.Deserialize<List<Manga>>(mangasJson.GetRawText());
				if (items != null)
				{
					_context.Mangas.RemoveRange(_context.Mangas);
					_context.Mangas.AddRange(items);
				}
			}

			// Chapters
			if (data.TryGetValue("Chapters", out var chaptersJson))
			{
				var items = JsonSerializer.Deserialize<List<Chapter>>(chaptersJson.GetRawText());
				if (items != null)
				{
					_context.Chapters.RemoveRange(_context.Chapters);
					_context.Chapters.AddRange(items);
				}
			}

			// ChapterPages
			if (data.TryGetValue("ChapterPages", out var pagesJson))
			{
				var items = JsonSerializer.Deserialize<List<ChapterPage>>(pagesJson.GetRawText());
				if (items != null)
				{
					_context.ChapterPages.RemoveRange(_context.ChapterPages);
					_context.ChapterPages.AddRange(items);
				}
			}

			// Users
			if (data.TryGetValue("Users", out var usersJson))
			{
				var items = JsonSerializer.Deserialize<List<User>>(usersJson.GetRawText());
				if (items != null)
				{
					_context.Users.RemoveRange(_context.Users);
					_context.Users.AddRange(items);
				}
			}

			// PfpDecorations
			if (data.TryGetValue("PfpDecorations", out var decorationsJson))
			{
				var items = JsonSerializer.Deserialize<List<PfpDecoration>>(decorationsJson.GetRawText());
				if (items != null)
				{
					_context.PfpDecorations.RemoveRange(_context.PfpDecorations);
					_context.PfpDecorations.AddRange(items);
				}
			}

			// UserTitles
			if (data.TryGetValue("UserTitles", out var titlesJson))
			{
				var items = JsonSerializer.Deserialize<List<UserTitle>>(titlesJson.GetRawText());
				if (items != null)
				{
					_context.UserTitles.RemoveRange(_context.UserTitles);
					_context.UserTitles.AddRange(items);
				}
			}

			// SiteSettings
			if (data.TryGetValue("SiteSettings", out var settingsJson))
			{
				var items = JsonSerializer.Deserialize<List<SiteSetting>>(settingsJson.GetRawText());
				if (items != null)
				{
					_context.SiteSettings.RemoveRange(_context.SiteSettings);
					_context.SiteSettings.AddRange(items);
				}
			}
			
			await _context.SaveChangesAsync();
			TempData["SuccessMessage"] = "Database imported successfully!";
		}
		catch (Exception ex)
		{
			TempData["ErrorMessage"] = "Import failed: " + ex.Message;
		}

		return RedirectToAction("ManageDatabase");
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ToggleMaintenance(bool enabled)
	{
		if (!IsCurrentUserAdmin()) return Forbid();

		var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "MaintenanceMode");
		if (setting == null)
		{
			setting = new SiteSetting { Key = "MaintenanceMode", Value = enabled.ToString().ToLower() };
			_context.SiteSettings.Add(setting);
		}
		else
		{
			setting.Value = enabled.ToString().ToLower();
		}

		await _context.SaveChangesAsync();
		TempData["SuccessMessage"] = $"Maintenance mode {(enabled ? "enabled" : "disabled")}.";
		return RedirectToAction("ManageDatabase");
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> FixDatabase()
	{
		if (!IsCurrentUserAdmin())
		{
			return Forbid();
		}
		try
		{
			using var command = _context.Database.GetDbConnection().CreateCommand();
			await _context.Database.OpenConnectionAsync();
			
			
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
			}
			
			command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'UserUnlockedDecoration' AND TABLE_SCHEMA = DATABASE();";
			if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
			{
				command.CommandText = @"
                        CREATE TABLE UserUnlockedDecoration (
                            Id INT AUTO_INCREMENT PRIMARY KEY,
                            UserId INT NOT NULL,
                            DecorationId INT NOT NULL,
                            UnlockedAt DATETIME(6) NOT NULL,
                            CONSTRAINT FK_UserUnlockedDecoration_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                            CONSTRAINT FK_UserUnlockedDecoration_Decorations_DecId FOREIGN KEY (DecorationId) REFERENCES PfpDecorations(Id) ON DELETE CASCADE,
                            UNIQUE KEY IX_UserUnlockedDecoration_UserId_DecorationId (UserId, DecorationId)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
				await command.ExecuteNonQueryAsync();
			}
			else
			{
				try
				{
					command.CommandText = "SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_NAME = 'UserUnlockedDecoration' AND INDEX_NAME = 'IX_UserUnlockedDecoration_UserId_DecorationId' AND TABLE_SCHEMA = DATABASE();";
					if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
					{
						command.CommandText = "ALTER TABLE UserUnlockedDecoration ADD UNIQUE KEY IX_UserUnlockedDecoration_UserId_DecorationId (UserId, DecorationId);";
						await command.ExecuteNonQueryAsync();
					}
				}
				catch { }
			}
			
			command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'UserUnlockedTitle' AND TABLE_SCHEMA = DATABASE();";
			if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
			{
				command.CommandText = @"
                        CREATE TABLE UserUnlockedTitle (
                            Id INT AUTO_INCREMENT PRIMARY KEY,
                            UserId INT NOT NULL,
                            TitleId INT NOT NULL,
                            UnlockedAt DATETIME(6) NOT NULL,
                            CONSTRAINT FK_UserUnlockedTitle_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                            CONSTRAINT FK_UserUnlockedTitle_Titles_TitleId FOREIGN KEY (TitleId) REFERENCES UserTitles(Id) ON DELETE CASCADE,
                            UNIQUE KEY IX_UserUnlockedTitle_UserId_TitleId (UserId, TitleId)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
				await command.ExecuteNonQueryAsync();
			}
			else
			{
				try
				{
					command.CommandText = "SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_NAME = 'UserUnlockedTitle' AND INDEX_NAME = 'IX_UserUnlockedTitle_UserId_TitleId' AND TABLE_SCHEMA = DATABASE();";
					if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
					{
						command.CommandText = "ALTER TABLE UserUnlockedTitle ADD UNIQUE KEY IX_UserUnlockedTitle_UserId_TitleId (UserId, TitleId);";
						await command.ExecuteNonQueryAsync();
					}
				}
				catch { }
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
			}
			
			command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Mangas' AND COLUMN_NAME = 'AniListId' AND TABLE_SCHEMA = DATABASE();";
			if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
			{
				command.CommandText = "ALTER TABLE Mangas ADD COLUMN AniListId INT NULL;";
				await command.ExecuteNonQueryAsync();
			}
			
			command.CommandText = "ALTER TABLE Chapters MODIFY COLUMN Title VARCHAR(300) NULL;";
			await command.ExecuteNonQueryAsync();
			
			command.CommandText = @"
              CREATE TABLE IF NOT EXISTS ChangelogEntries (
                  Id INT AUTO_INCREMENT PRIMARY KEY,
                  Title VARCHAR(200) NOT NULL,
                  Content TEXT NOT NULL,
                  CreatedAt DATETIME NOT NULL
              );";
			await command.ExecuteNonQueryAsync();
			
			try
			{
				command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'FollowChangelog' AND TABLE_SCHEMA = DATABASE();";
				if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
				{
					command.CommandText = "ALTER TABLE Users ADD COLUMN FollowChangelog TINYINT(1) NOT NULL DEFAULT 1;";
					await command.ExecuteNonQueryAsync();
				}
			}
			catch { }
			
			try
			{
				string[] columns = { 
					"CustomPrimaryColor VARCHAR(50) NULL", 
					"CustomAccentColor VARCHAR(50) NULL", 
					"CustomCss TEXT NULL", 
					"DisableFeaturedBanners TINYINT(1) NOT NULL DEFAULT 0", 
					"ShowAllFeaturedAsCovers TINYINT(1) NOT NULL DEFAULT 0", 
					"CustomBackgroundUrl VARCHAR(500) NULL", 
					"SiteOpacity DOUBLE NOT NULL DEFAULT 1.0" 
				};
				
				foreach (string col in columns)
				{
					string colName = col.Split(' ')[0];
					command.CommandText = $"SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = '{colName}' AND TABLE_SCHEMA = DATABASE();";
					if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
					{
						command.CommandText = $"ALTER TABLE Users ADD COLUMN {col};";
						await command.ExecuteNonQueryAsync();
					}
				}
			}
			catch { }

			
			try
			{
				command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'CommentReactions' AND TABLE_SCHEMA = DATABASE();";
				if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
				{
					command.CommandText = @"
						CREATE TABLE CommentReactions (
							Id INT AUTO_INCREMENT PRIMARY KEY,
							CommentId INT NOT NULL,
							UserId INT NOT NULL,
							IsLike TINYINT(1) NOT NULL,
							CreatedAt DATETIME(6) NOT NULL,
							CONSTRAINT FK_CommentReactions_ChapterComments_CommentId FOREIGN KEY (CommentId) REFERENCES ChapterComments(Id) ON DELETE CASCADE,
							CONSTRAINT FK_CommentReactions_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
							UNIQUE KEY IX_CommentReactions_CommentId_UserId (CommentId, UserId)
						) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
					await command.ExecuteNonQueryAsync();
				}
				else
				{
					
					string[] reactionCols = { "CommentId INT NOT NULL", "UserId INT NOT NULL", "IsLike TINYINT(1) NOT NULL", "CreatedAt DATETIME(6) NOT NULL" };
					foreach (var col in reactionCols)
					{
						string colName = col.Split(' ')[0];
						command.CommandText = $"SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'CommentReactions' AND COLUMN_NAME = '{colName}' AND TABLE_SCHEMA = DATABASE();";
						if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
						{
							command.CommandText = $"ALTER TABLE CommentReactions ADD COLUMN {col};";
							await command.ExecuteNonQueryAsync();
						}
					}

					
					command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'CommentReactions' AND COLUMN_NAME = 'Type' AND TABLE_SCHEMA = DATABASE();";
					if (Convert.ToInt32(await command.ExecuteScalarAsync()) > 0)
					{
						command.CommandText = "ALTER TABLE CommentReactions MODIFY COLUMN Type INT NOT NULL DEFAULT 0;";
						await command.ExecuteNonQueryAsync();
					}
				}
			}
			catch { }

			
			try
			{
				command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'Notifications' AND TABLE_SCHEMA = DATABASE();";
				if (Convert.ToInt32(await command.ExecuteScalarAsync()) > 0)
				{
					
					command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Notifications' AND COLUMN_NAME = 'Type' AND TABLE_SCHEMA = DATABASE();";
					if (Convert.ToInt32(await command.ExecuteScalarAsync()) > 0)
					{
						command.CommandText = "ALTER TABLE Notifications MODIFY COLUMN Type INT NOT NULL DEFAULT 0;";
						await command.ExecuteNonQueryAsync();
					}

					
					string[] notifCols = { 
						"RelatedMangaId INT NULL", 
						"RelatedChapterId INT NULL", 
						"RelatedCommentId INT NULL", 
						"TriggerUserId INT NULL" 
					};
					foreach (var col in notifCols)
					{
						string colName = col.Split(' ')[0];
						command.CommandText = $"SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'Notifications' AND COLUMN_NAME = '{colName}' AND TABLE_SCHEMA = DATABASE();";
						if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
						{
							command.CommandText = $"ALTER TABLE Notifications ADD COLUMN {col};";
							await command.ExecuteNonQueryAsync();
						}
					}
				}
			}
			catch { }

			
			try
			{
				
				command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'ChapterViews' AND COLUMN_NAME = 'Device' AND TABLE_SCHEMA = DATABASE();";
				if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
				{
					command.CommandText = "ALTER TABLE ChapterViews ADD COLUMN Device VARCHAR(50) NOT NULL DEFAULT 'Desktop';";
					await command.ExecuteNonQueryAsync();
				}

				
				command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = 'ChapterViews' AND COLUMN_NAME = 'SessionId' AND TABLE_SCHEMA = DATABASE();";
				if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
				{
					command.CommandText = "ALTER TABLE ChapterViews ADD COLUMN SessionId VARCHAR(100) NULL;";
					await command.ExecuteNonQueryAsync();
				}

				
				command.CommandText = "SELECT IS_NULLABLE FROM information_schema.COLUMNS WHERE TABLE_NAME = 'ChapterViews' AND COLUMN_NAME = 'UserId' AND TABLE_SCHEMA = DATABASE();";
				string? isNullable = (await command.ExecuteScalarAsync())?.ToString();
				if (isNullable != null && isNullable.ToUpper() == "NO")
				{
					
					command.CommandText = "ALTER TABLE ChapterViews MODIFY COLUMN UserId INT NULL;";
					await command.ExecuteNonQueryAsync();
				}
			}
			catch { }

			
			try
			{
				command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'CommentReactions' AND TABLE_SCHEMA = DATABASE();";
				if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
				{
					command.CommandText = @"
						CREATE TABLE CommentReactions (
							Id INT AUTO_INCREMENT PRIMARY KEY,
							CommentId INT NOT NULL,
							UserId INT NOT NULL,
							IsLike TINYINT(1) NOT NULL,
							CreatedAt DATETIME(6) NOT NULL,
							CONSTRAINT FK_CommentReactions_ChapterComments_CommentId FOREIGN KEY (CommentId) REFERENCES ChapterComments(Id) ON DELETE CASCADE,
							CONSTRAINT FK_CommentReactions_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
							UNIQUE KEY IX_CommentReactions_CommentId_UserId (CommentId, UserId)
						) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
					await command.ExecuteNonQueryAsync();
				}
				else
				{
					
					try
					{
						command.CommandText = "SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_NAME = 'CommentReactions' AND INDEX_NAME = 'IX_CommentReactions_CommentId_UserId' AND TABLE_SCHEMA = DATABASE();";
						if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
						{
							command.CommandText = "ALTER TABLE CommentReactions ADD UNIQUE KEY IX_CommentReactions_CommentId_UserId (CommentId, UserId);";
							await command.ExecuteNonQueryAsync();
						}
					}
					catch { }
				}
			}
			catch { }
			
			TempData["SuccessMessage"] = "Database fixes applied successfully!";
		}
		catch (Exception ex)
		{
			TempData["ErrorMessage"] = "Error updating database: " + ex.Message;
		}
		return RedirectToAction("Index");
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MangaReader.Data;
using MangaReader.Models;
using MangaReader.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MangaReader.Controllers;

public class SeriesController(ApplicationDbContext context, ILogger<SeriesController> logger, IBookmarkService bookmarkService) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<SeriesController> _logger = logger;
    private readonly IBookmarkService _bookmarkService = bookmarkService;

	public async Task<IActionResult> Index(string search = "", string genre = "", string status = "")
	{
		IQueryable<Manga> query = _context.Mangas.Include(m => m.Chapters).AsQueryable();
		if (!string.IsNullOrEmpty(search))
		{
			query = query.Where(m => m.Title.Contains(search) || m.Author.Contains(search));
		}
		if (!string.IsNullOrEmpty(genre))
		{
			query = query.Where(m => m.Genre.Contains(genre));
		}
		if (!string.IsNullOrEmpty(status))
		{
			query = query.Where(m => m.Status == status);
		}
		List<Manga> result = await query.OrderByDescending(m => m.UpdatedAt).ToListAsync();
		ViewBag.Search = search;
		ViewBag.Genre = genre;
		ViewBag.Status = status;
		return View(result);
	}

	public async Task<IActionResult> Detail(int id)
	{
		Manga? manga = await _context.Mangas
			.Include(m => m.Chapters.OrderByDescending(c => c.ChapterNumber))
				.ThenInclude(c => c.Pages.OrderBy(p => p.PageNumber))
			.FirstOrDefaultAsync(m => m.Id == id);

		if (manga == null)
		{
			return NotFound();
		}

		if (User.Identity?.IsAuthenticated ?? false)
		{
			string? userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			if (int.TryParse(userIdStr, out var userId))
			{
				UserRating? userRating = await _context.UserRatings.FirstOrDefaultAsync(r => r.UserId == userId && r.MangaId == id);
				ViewBag.UserRating = userRating?.Rating ?? 0;
			}
		}
		return View(manga);
	}

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> RateManga([FromBody] RatingRequest request)
    {
        if (request.Rating < 1 || request.Rating > 10)
        {
            return BadRequest("Rating must be between 1 and 10.");
        }

        string? userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var manga = await _context.Mangas.FindAsync(request.MangaId);
        if (manga == null)
        {
            return NotFound("Manga not found.");
        }

        var existingRating = await _context.UserRatings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.MangaId == request.MangaId);

        if (existingRating != null)
        {
            existingRating.Rating = request.Rating;
            existingRating.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.UserRatings.Add(new UserRating
            {
                UserId = userId,
                MangaId = request.MangaId,
                Rating = request.Rating,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        
        var allRatings = await _context.UserRatings
            .Where(r => r.MangaId == request.MangaId)
            .Select(r => r.Rating)
            .ToListAsync();

        if (allRatings.Any())
        {
            
            double average = allRatings.Average();
            manga.Rating = (int)Math.Round(average * 10);
            await _context.SaveChangesAsync();
        }

        return Ok(new { success = true, averageRating = manga.Rating });
    }

    public class RatingRequest
    {
        public int MangaId { get; set; }
        public int Rating { get; set; }
    }

	public async Task<IActionResult> ReadChapter(int id)
	{
		Chapter? chapter = null;
		try
		{
			chapter = await _context.Chapters
				.Include(c => c.Manga)
				.Include(c => c.Pages.OrderBy(p => p.PageNumber))
				.Include(c => c.Comments)
					.ThenInclude(cc => cc.User)
						.ThenInclude(u => u.EquippedTitle)
				.Include(c => c.Comments)
					.ThenInclude(cc => cc.User)
						.ThenInclude(u => u.EquippedDecoration)
				.Include(c => c.Comments)
					.ThenInclude(cc => cc.Reactions)
				.Include(c => c.Comments)
					.ThenInclude(cc => cc.RepliedToUser)
				.FirstOrDefaultAsync(c => c.Id == id);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading chapter {ChapterId} with full details, falling back to basic load", id);
			chapter = await _context.Chapters
				.Include(c => c.Manga)
				.Include(c => c.Pages.OrderBy(p => p.PageNumber))
				.Include(c => c.Comments)
					.ThenInclude(cc => cc.User)
						.ThenInclude(u => u.EquippedTitle)
				.Include(c => c.Comments)
					.ThenInclude(cc => cc.User)
						.ThenInclude(u => u.EquippedDecoration)
				.Include(c => c.Comments)
					.ThenInclude(cc => cc.Reactions)
				.Include(c => c.Comments)
					.ThenInclude(cc => cc.RepliedToUser)
				.FirstOrDefaultAsync(c => c.Id == id);
		}

		if (chapter == null)
		{
			return NotFound();
		}

		try
		{
			ViewBag.EnableDecorations = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "EnableDecorations"))?.Value ?? "true";
			ViewBag.EnableTitles = (await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "EnableTitles"))?.Value ?? "true";
		}
		catch
		{
			ViewBag.EnableDecorations = "true";
			ViewBag.EnableTitles = "true";
		}

		bool isAdmin = User.FindFirst("IsAdmin")?.Value == "True" || User.FindFirst("IsSubAdmin")?.Value == "True";
		
		if (!isAdmin)
		{
			string userAgent = Request.Headers["User-Agent"].ToString().ToLower();
			string device = "Desktop";
			if (userAgent.Contains("mobi")) device = "Mobile";
			else if (userAgent.Contains("tablet") || userAgent.Contains("ipad")) device = "Tablet";

			int? userId = null;
			if (User.Identity?.IsAuthenticated ?? false)
			{
				userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
			}

			
			bool shouldRecordView = false;
			if (userId.HasValue)
			{
				
				
				if (!await _context.ChapterViews.AnyAsync(cv => cv.UserId == userId && cv.ChapterId == id))
				{
					shouldRecordView = true;
					await AwardXpAsync(userId.Value, 10);
				}
			}
			else
			{
				string sessionKey = $"ViewedChapter_{id}";
				if (string.IsNullOrEmpty(HttpContext.Session.GetString(sessionKey)))
				{
					shouldRecordView = true;
					HttpContext.Session.SetString(sessionKey, "1");
				}
			}

			if (shouldRecordView)
			{
				chapter.ViewCount++;
				_context.ChapterViews.Add(new ChapterView
				{
					UserId = userId,
					ChapterId = id,
					Device = device,
					ViewedAt = DateTime.UtcNow,
					SessionId = HttpContext.Session.Id
				});
				await _context.SaveChangesAsync();
			}
		}

		List<Chapter> allChapters = await _context.Chapters
			.Where(c => c.MangaId == chapter.MangaId)
			.OrderBy(c => c.ChapterNumber)
			.ToListAsync();

		ViewBag.AllChapters = allChapters;
		string emojiPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "emojis.json");
		if (!System.IO.File.Exists(emojiPath))
		{
			ViewBag.EmojisJson = "[]";
		}
		else
		{
			string emojiJson = await System.IO.File.ReadAllTextAsync(emojiPath);
			ViewBag.EmojisJson = emojiJson;
		}
		return View(chapter);
	}

	private async Task AwardXpAsync(int userId, int amount)
	{
		User? user = await _context.Users
			.Include(u => u.UnlockedDecorations)
			.Include(u => u.UnlockedTitles)
			.FirstOrDefaultAsync(u => u.Id == userId);

		if (user == null)
		{
			return;
		}

		user.XP += amount;
		bool leveledUp = false;
		int oldLevel = user.Level;

		while (true)
		{
			int xpNeeded = (int)(100.0 * Math.Pow(1.5, user.Level - 1));
			if (user.XP < xpNeeded)
			{
				break;
			}
			user.XP -= xpNeeded;
			user.Level++;
			leveledUp = true;
		}

		if (!leveledUp)
		{
			return;
		}

		List<PfpDecoration> decorationsToUnlock = await _context.PfpDecorations
			.Where(d => d.LevelRequirement > oldLevel && d.LevelRequirement <= user.Level && !d.IsLocked)
			.ToListAsync();

		List<UserTitle> titlesToUnlock = await _context.UserTitles
			.Where(t => t.LevelRequirement > oldLevel && t.LevelRequirement <= user.Level && !t.IsLocked)
			.ToListAsync();

		foreach (PfpDecoration dec in decorationsToUnlock)
		{
			if (!user.UnlockedDecorations.Any(ud => ud.DecorationId == dec.Id))
			{
				_context.Set<UserUnlockedDecoration>().Add(new UserUnlockedDecoration
				{
					UserId = user.Id,
					DecorationId = dec.Id,
					UnlockedAt = DateTime.UtcNow
				});
			}
		}

		foreach (UserTitle title in titlesToUnlock)
		{
			if (!user.UnlockedTitles.Any(ut => ut.TitleId == title.Id))
			{
				_context.Set<UserUnlockedTitle>().Add(new UserUnlockedTitle
				{
					UserId = user.Id,
					TitleId = title.Id,
					UnlockedAt = DateTime.UtcNow
				});
			}
		}

		INotificationService? notificationService = HttpContext.RequestServices.GetService<INotificationService>();
		if (notificationService != null)
		{
			await notificationService.CreateNotificationAsync(user.Id, NotificationType.System, $"Congratulations! You reached Level {user.Level}!", null, null, null, user.Id);
			if (decorationsToUnlock.Any() || titlesToUnlock.Any())
			{
				await notificationService.CreateNotificationAsync(user.Id, NotificationType.Reward, $"You unlocked {decorationsToUnlock.Count} decorations and {titlesToUnlock.Count} titles!", null, null, null, user.Id);
			}
		}
	}

	[Authorize]
	[HttpPost]
	public async Task<IActionResult> AddComment([FromBody] CommentRequest request, [FromServices] INotificationService notificationService)
	{
		if (User.Identity?.IsAuthenticated != true)
		{
			return Json(new
			{
				success = false,
				message = "Please log in to comment."
			});
		}

		int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
		string username = User.Identity.Name ?? "Someone";

		if (string.IsNullOrWhiteSpace(request.Content))
		{
			return Json(new
			{
				success = false,
				message = "Comment cannot be empty."
			});
		}

		if (request.Content.Length > 1000)
		{
			return Json(new
			{
				success = false,
				message = "Comment exceeds 1000 characters."
			});
		}

		Chapter? chapter = await _context.Chapters.Include(c => c.Manga).FirstOrDefaultAsync(c => c.Id == request.ChapterId);
		if (chapter == null)
		{
			return Json(new
			{
				success = false,
				message = "Chapter not found."
			});
		}

		int? parentId = request.ParentId > 0 ? request.ParentId : null;
		int? repliedToUserId = null;
		string content = request.Content.Trim();

		if (parentId.HasValue)
		{
			ChapterComment? parent = await _context.ChapterComments.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == parentId.Value);
			if (parent != null)
			{
				repliedToUserId = parent.UserId;
				if (parent.ParentCommentId.HasValue)
				{
					parentId = parent.ParentCommentId;
				}
			}
		}

		ChapterComment comment = new ChapterComment
		{
			ChapterId = request.ChapterId,
			UserId = userId,
			Content = content,
			CreatedAt = DateTime.UtcNow,
			ParentCommentId = parentId,
			RepliedToUserId = repliedToUserId
		};

		_context.ChapterComments.Add(comment);
		await _context.SaveChangesAsync();

		try
		{
			bool isAdmin = User.FindFirst("IsAdmin")?.Value == "True" || User.FindFirst("IsSubAdmin")?.Value == "True";
			bool isReply = parentId.HasValue;
			bool isSelfReply = repliedToUserId.HasValue && repliedToUserId.Value == userId;
			bool shouldAwardXP = false;

			if (!isAdmin)
			{
				if (!isReply)
				{
					if (!await _context.ChapterComments.AnyAsync(cc => cc.UserId == userId && cc.ChapterId == request.ChapterId && cc.ParentCommentId == null && cc.Id != comment.Id))
					{
						shouldAwardXP = true;
					}
				}
				else if (!isSelfReply && !await _context.ChapterComments.AnyAsync(cc => cc.UserId == userId && cc.ChapterId == request.ChapterId && cc.ParentCommentId != null && cc.RepliedToUserId != userId && cc.Id != comment.Id))
				{
					shouldAwardXP = true;
				}
			}

			if (shouldAwardXP)
			{
				string cleanContent = StripMarkdown(content);
				int baseXP = 10;
				int lengthXP = cleanContent.Length / 10;
				int totalXP = Math.Min(baseXP + lengthXP, 100);
				await AwardXpAsync(userId, totalXP);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error awarding XP for comment");
		}

		if (repliedToUserId.HasValue && repliedToUserId.Value != userId)
		{
			await notificationService.CreateNotificationAsync(repliedToUserId.Value, NotificationType.Community, $"{username} replied to your comment on {chapter.Manga?.Title} - Ch. {chapter.ChapterNumber}", chapter.MangaId, chapter.Id, comment.Id, userId);
		}
		else if (comment.ParentCommentId.HasValue)
		{
			ChapterComment? parentComment = await _context.ChapterComments.FirstOrDefaultAsync(c => c.Id == comment.ParentCommentId.Value);
			if (parentComment != null && parentComment.UserId != userId && parentComment.UserId != repliedToUserId)
			{
				await notificationService.CreateNotificationAsync(parentComment.UserId, NotificationType.Community, $"{username} commented on your post on {chapter.Manga?.Title} - Ch. {chapter.ChapterNumber}", chapter.MangaId, chapter.Id, comment.Id, userId);
			}
		}

		return Json(new
		{
			success = true,
			message = "Comment posted.",
			commentId = comment.Id
		});
	}

	private string StripMarkdown(string content)
	{
		if (string.IsNullOrEmpty(content))
		{
			return string.Empty;
		}
		string result = Regex.Replace(content, "\\[([^\\]]+)\\]\\([^\\)]+\\)", "$1");
		result = Regex.Replace(result, "(\\*\\*|__)(.*?)\\1", "$2");
		result = Regex.Replace(result, "(\\*|_)(.*?)\\1", "$2");
		result = Regex.Replace(result, "(`{1,3})(.*?)\\1", "$2");
		result = Regex.Replace(result, "^\\s*>\\s*", "", RegexOptions.Multiline);
		return result.Trim();
	}

	[Authorize]
	[HttpPost]
	public async Task<IActionResult> ReactToComment([FromBody] ReactionRequest request, [FromServices] INotificationService notificationService)
	{
		int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
		string username = User.Identity?.Name ?? "Someone";

		_logger.LogInformation("User {UserId} ({Username}) is reacting to comment {CommentId} (IsLike: {IsLike})", userId, username, request.CommentId, request.IsLike);

		ChapterComment? comment = await _context.ChapterComments
			.Include(c => c.Chapter!)
				.ThenInclude(ch => ch.Manga)
			.FirstOrDefaultAsync(c => c.Id == request.CommentId);

		if (comment == null)
		{
			return Json(new
			{
				success = false,
				message = "Comment not found."
			});
		}

		CommentReaction? existingReaction = await _context.CommentReactions.FirstOrDefaultAsync(r => r.CommentId == request.CommentId && r.UserId == userId);
		if (existingReaction != null)
		{
			if (existingReaction.IsLike == request.IsLike)
			{
				_context.CommentReactions.Remove(existingReaction);
			}
			else
			{
				existingReaction.IsLike = request.IsLike;
				existingReaction.CreatedAt = DateTime.UtcNow;
			}
		}
		else
		{
			CommentReaction reaction = new CommentReaction
			{
				CommentId = request.CommentId,
				UserId = userId,
				IsLike = request.IsLike,
				CreatedAt = DateTime.UtcNow
			};
			_context.CommentReactions.Add(reaction);

			if (request.IsLike && comment.UserId != userId)
			{
				await notificationService.CreateNotificationAsync(comment.UserId, NotificationType.Community, $"{username} liked your comment on {comment.Chapter?.Manga?.Title} - Ch. {comment.Chapter?.ChapterNumber}", comment.Chapter?.MangaId, comment.ChapterId, comment.Id, userId);
			}
		}

		await _context.SaveChangesAsync();
		var userReaction = await _context.CommentReactions.FirstOrDefaultAsync(r => r.CommentId == request.CommentId && r.UserId == userId);
		
		return Json(new
		{
			success = true,
			likes = await _context.CommentReactions.CountAsync(r => r.CommentId == request.CommentId && r.IsLike),
			dislikes = await _context.CommentReactions.CountAsync(r => r.CommentId == request.CommentId && !r.IsLike),
			isLiked = userReaction != null && userReaction.IsLike,
			isDisliked = userReaction != null && !userReaction.IsLike
		});
	}

	[Authorize]
	[HttpPost]
	public async Task<IActionResult> DeleteComment([FromBody] DeleteCommentRequest request)
	{
		if (User.Identity?.IsAuthenticated != true)
		{
			return Json(new
			{
				success = false,
				message = "Please log in."
			});
		}

		int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
		bool isAdmin = User.FindFirst("IsAdmin")?.Value == "True" || User.FindFirst("IsSubAdmin")?.Value == "True";

		ChapterComment? comment = await _context.ChapterComments.FirstOrDefaultAsync(c => c.Id == request.CommentId);
		if (comment == null)
		{
			return Json(new
			{
				success = false,
				message = "Comment not found."
			});
		}

		if (!isAdmin && comment.UserId != userId)
		{
			return Json(new
			{
				success = false,
				message = "You do not have permission to delete this comment."
			});
		}

		_context.ChapterComments.Remove(comment);
		await _context.SaveChangesAsync();

		return Json(new
		{
			success = true,
			message = "Comment deleted."
		});
	}

	[Authorize]
	[HttpPost]
	public async Task<IActionResult> UpdateComment([FromBody] UpdateCommentRequest request)
	{
		if (User.Identity?.IsAuthenticated != true)
		{
			return Json(new
			{
				success = false,
				message = "Please log in."
			});
		}

		int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
		bool isAdmin = User.FindFirst("IsAdmin")?.Value == "True" || User.FindFirst("IsSubAdmin")?.Value == "True";

		ChapterComment? comment = await _context.ChapterComments.FirstOrDefaultAsync(c => c.Id == request.CommentId);
		if (comment == null)
		{
			return Json(new
			{
				success = false,
				message = "Comment not found."
			});
		}

		if (!isAdmin && comment.UserId != userId)
		{
			return Json(new
			{
				success = false,
				message = "You do not have permission to edit this comment."
			});
		}

		if (string.IsNullOrWhiteSpace(request.Content))
		{
			return Json(new
			{
				success = false,
				message = "Comment cannot be empty."
			});
		}

		comment.Content = request.Content.Trim();
		await _context.SaveChangesAsync();

		return Json(new
		{
			success = true,
			message = "Comment updated."
		});
	}

	[Authorize]
	public IActionResult Bookmarks()
	{
		return RedirectToAction("Profile", "Auth");
	}

	[Authorize]
	[HttpPost]
	public async Task<IActionResult> AddBookmark([FromBody] BookmarkRequest request)
	{
		if (User.Identity?.IsAuthenticated != true)
		{
			return Json(new
			{
				success = false,
				message = "Please log in to bookmark manga."
			});
		}

		int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
		if (await _bookmarkService.AddOrUpdateBookmarkAsync(userId, request.MangaId, (BookmarkStatus)request.Status))
		{
			return Json(new
			{
				success = true,
				message = "Bookmark updated successfully."
			});
		}

		return Json(new
		{
			success = false,
			message = "Failed to update bookmark."
		});
	}

	[Authorize]
	[HttpPost]
	public async Task<IActionResult> RemoveBookmark([FromBody] BookmarkRequest request)
	{
		if (User.Identity?.IsAuthenticated != true)
		{
			return Json(new
			{
				success = false,
				message = "Please log in."
			});
		}

		int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
		if (await _bookmarkService.RemoveBookmarkAsync(userId, request.MangaId))
		{
			return Json(new
			{
				success = true,
				message = "Bookmark removed."
			});
		}

		return Json(new
		{
			success = false,
			message = "Bookmark not found."
		});
	}

	[Authorize]
	public async Task<IActionResult> GetUserBookmark(int mangaId)
	{
		if (User.Identity?.IsAuthenticated != true)
		{
			return Json(new
			{
				bookmarked = false
			});
		}

		int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
		UserBookmark? bookmark = await _bookmarkService.GetBookmarkAsync(userId, mangaId);
		return Json(new
		{
			bookmarked = bookmark != null,
			status = bookmark?.Status ?? BookmarkStatus.PlanToRead
		});
	}
}

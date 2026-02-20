using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaReader.Data;
using MangaReader.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MangaReader.Services;

public class ChapterService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, INotificationService notificationService) : IChapterService
{
	private readonly ApplicationDbContext _context = context;

	private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

	private readonly INotificationService _notificationService = notificationService;

	public async Task CreateChapterAsync(Chapter chapter, List<IFormFile> pages, string pageUrls)
	{
		var strategy = _context.Database.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async () =>
		{
			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				chapter.CreatedAt = DateTime.UtcNow;
				chapter.UpdatedAt = DateTime.UtcNow;
				chapter.ViewCount = 0;
				
				var manga = await _context.Mangas.FindAsync(chapter.MangaId);
				if (manga != null)
				{
					manga.UpdatedAt = DateTime.UtcNow;
					manga.LastChapterDate = DateTime.UtcNow;
				}
				
				_context.Chapters.Add(chapter);
				await _context.SaveChangesAsync();
				
				if (manga != null)
				{
					var userIds = await _context.UserBookmarks
						.Where(b => b.MangaId == manga.Id)
						.Select(b => b.UserId)
						.ToListAsync();

					foreach (var userId in userIds)
					{
						await _notificationService.CreateNotificationAsync(userId, NotificationType.Comic, $"New chapter released: {manga.Title} - Ch. {chapter.ChapterNumber}", manga.Id, chapter.Id);
					}
				}
				
				int pageNumber = 1;
				var newPages = new List<ChapterPage>();
				
				if (pages != null && pages.Count > 0)
				{
					string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "manga", chapter.MangaId.ToString(), chapter.Id.ToString());
					
					if (!Directory.Exists(uploadsFolder))
					{
						Directory.CreateDirectory(uploadsFolder);
					}
					
					foreach (var page in pages)
					{
						if (page.Length > 0)
						{
							var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
							string extension = Path.GetExtension(page.FileName).ToLowerInvariant();
							
							if (allowedExtensions.Contains(extension) && page.Length <= 10485760)
							{
								string uniqueFileName = $"{pageNumber:000}_{Guid.NewGuid()}{extension}";
								string path = Path.Combine(uploadsFolder, uniqueFileName);
								
								using (var fileStream = new FileStream(path, FileMode.Create))
								{
									await page.CopyToAsync(fileStream);
								}
								
								newPages.Add(new ChapterPage
								{
									ChapterId = chapter.Id,
									PageNumber = pageNumber++,
									ImageUrl = $"/uploads/manga/{chapter.MangaId}/{chapter.Id}/{uniqueFileName}",
									CreatedAt = DateTime.UtcNow
								});
							}
						}
					}
				}
				if (!string.IsNullOrWhiteSpace(pageUrls))
				{
					var urls = pageUrls.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
						.Select(u => u.Trim())
						.Where(u => !string.IsNullOrWhiteSpace(u));

					foreach (var url in urls)
					{
						newPages.Add(new ChapterPage
						{
							ChapterId = chapter.Id,
							PageNumber = pageNumber++,
							ImageUrl = url,
							CreatedAt = DateTime.UtcNow
						});
					}
				}

				if (newPages.Any())
				{
					_context.ChapterPages.AddRange(newPages);
					await _context.SaveChangesAsync();
				}

				await transaction.CommitAsync();
			}
			catch (Exception)
			{
				await transaction.RollbackAsync();
				throw;
			}
		});
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MangaReader.Data;
using MangaReader.Models;
using Microsoft.EntityFrameworkCore;

namespace MangaReader.Services;

public class BookmarkService(ApplicationDbContext context) : IBookmarkService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<IEnumerable<UserBookmark>> GetUserBookmarksAsync(int userId)
    {
        return await _context.UserBookmarks
            .Include(b => b.Manga)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.UpdatedAt)
            .ToListAsync();
    }

    public async Task<bool> AddOrUpdateBookmarkAsync(int userId, int mangaId, BookmarkStatus status)
    {
        if (await _context.Mangas.FindAsync(mangaId) == null)
        {
            return false;
        }

        var userBookmark = await _context.UserBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.MangaId == mangaId);

        if (userBookmark != null)
        {
            userBookmark.Status = status;
            userBookmark.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.UserBookmarks.Add(new UserBookmark
            {
                UserId = userId,
                MangaId = mangaId,
                Status = status,
                AddedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveBookmarkAsync(int userId, int mangaId)
    {
        var userBookmark = await _context.UserBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.MangaId == mangaId);

        if (userBookmark != null)
        {
            _context.UserBookmarks.Remove(userBookmark);
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<UserBookmark?> GetBookmarkAsync(int userId, int mangaId)
    {
        return await _context.UserBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.MangaId == mangaId);
    }
}

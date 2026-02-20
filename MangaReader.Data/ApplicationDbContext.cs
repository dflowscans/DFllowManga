using MangaReader.Models;
using Microsoft.EntityFrameworkCore;

namespace MangaReader.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<Manga> Mangas => Set<Manga>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<ChapterPage> ChapterPages => Set<ChapterPage>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserBookmark> UserBookmarks => Set<UserBookmark>();
    public DbSet<UserRating> UserRatings => Set<UserRating>();
    public DbSet<ChapterComment> ChapterComments => Set<ChapterComment>();
    public DbSet<ChapterView> ChapterViews => Set<ChapterView>();
    public DbSet<PfpDecoration> PfpDecorations => Set<PfpDecoration>();
    public DbSet<UserTitle> UserTitles => Set<UserTitle>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<CommentReaction> CommentReactions => Set<CommentReaction>();
    public DbSet<UserUnlockedDecoration> UserUnlockedDecorations => Set<UserUnlockedDecoration>();
    public DbSet<UserUnlockedTitle> UserUnlockedTitles => Set<UserUnlockedTitle>();
    public DbSet<ChangelogEntry> ChangelogEntries => Set<ChangelogEntry>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Manga>()
            .HasMany(m => m.Chapters)
            .WithOne(c => c.Manga)
            .HasForeignKey(c => c.MangaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Chapter>()
            .HasMany(c => c.Pages)
            .WithOne(p => p.Chapter)
            .HasForeignKey(p => p.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Chapter>()
            .HasMany(c => c.Comments)
            .WithOne(cc => cc.Chapter)
            .HasForeignKey(cc => cc.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChapterComment>()
            .HasOne(cc => cc.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(cc => cc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChapterComment>()
            .HasOne(cc => cc.RepliedToUser)
            .WithMany()
            .HasForeignKey(cc => cc.RepliedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Chapter>()
            .Property(c => c.ChapterNumber)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Manga>()
            .HasIndex(m => m.IsFeatured);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<UserRating>()
            .HasIndex(r => new { r.UserId, r.MangaId })
            .IsUnique();

        modelBuilder.Entity<ChapterView>()
            .HasIndex(cv => new { cv.UserId, cv.ChapterId });

        modelBuilder.Entity<CommentReaction>()
            .HasIndex(cr => new { cr.CommentId, cr.UserId })
            .IsUnique();

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.TriggerUser)
            .WithMany()
            .HasForeignKey(n => n.TriggerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserUnlockedDecoration>()
            .HasIndex(ud => new { ud.UserId, ud.DecorationId })
            .IsUnique();

        modelBuilder.Entity<UserUnlockedTitle>()
            .HasIndex(ut => new { ut.UserId, ut.TitleId })
            .IsUnique();

        
        modelBuilder.Entity<PfpDecoration>().HasData(
            new PfpDecoration { Id = 1, Name = "Basic Frame", ImageUrl = "/images/decorations/basic.png", Price = 0, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PfpDecoration { Id = 2, Name = "Gold Frame", ImageUrl = "/images/decorations/gold.png", Price = 100, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MangaReader.Models;

public class User
{
	public int Id { get; set; }

	[Required(ErrorMessage = "Username is required")]
	[StringLength(100, MinimumLength = 3)]
	public required string Username { get; set; }

	[Required(ErrorMessage = "Password is required")]
	[StringLength(255, MinimumLength = 6)]
	public required string PasswordHash { get; set; }

	public bool IsAdmin { get; set; }

	public bool IsSubAdmin { get; set; }

	public bool IsActive { get; set; } = true;

	public DateTime CreatedAt { get; set; }

	public DateTime UpdatedAt { get; set; }

	public ICollection<UserBookmark> Bookmarks { get; set; } = new List<UserBookmark>();

	public ICollection<ChapterComment> Comments { get; set; } = new List<ChapterComment>();

	public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

	public virtual ICollection<CommentReaction> CommentReactions { get; set; } = new List<CommentReaction>();

	public virtual ICollection<UserUnlockedDecoration> UnlockedDecorations { get; set; } = new List<UserUnlockedDecoration>();

	public virtual ICollection<UserUnlockedTitle> UnlockedTitles { get; set; } = new List<UserUnlockedTitle>();

	[StringLength(500)]
	public string? AvatarUrl { get; set; }

	public string? ProfilePicture { get; set; }

	public string? Bio { get; set; }

	public int XP { get; set; }

	public int Level { get; set; } = 1;

	public int? EquippedDecorationId { get; set; }

	[ForeignKey("EquippedDecorationId")]
	public virtual PfpDecoration? EquippedDecoration { get; set; }

	public int? EquippedTitleId { get; set; }

	[ForeignKey("EquippedTitleId")]
	public virtual UserTitle? EquippedTitle { get; set; }

	public bool HideReadingList { get; set; }

	public bool FollowChangelog { get; set; } = true;

	public string? CustomPrimaryColor { get; set; }

	public string? CustomAccentColor { get; set; }

	public string? CustomBackgroundColor { get; set; }

	public string? CustomNavbarColor { get; set; }

	public string? CustomTextColor { get; set; }

 	public string? CustomCss { get; set; }

	public bool DisableFeaturedBanners { get; set; }

	public bool ShowAllFeaturedAsCovers { get; set; }

	public string? CustomBackgroundUrl { get; set; }

	public double? SiteOpacity { get; set; } = 1.0;

	public bool IgnoreSuggestiveWarnings { get; set; }
}

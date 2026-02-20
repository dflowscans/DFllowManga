using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MangaReader.Models;

public class Notification
{
	public int Id { get; set; }

	public int UserId { get; set; }

	[Required]
	public NotificationType Type { get; set; }

	[Required]
	[StringLength(500)]
	public required string Message { get; set; }

	public bool IsRead { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public int? RelatedMangaId { get; set; }

	public int? RelatedChapterId { get; set; }

	public int? RelatedCommentId { get; set; }

	public int? TriggerUserId { get; set; }

	[ForeignKey("UserId")]
	public virtual User? User { get; set; }

	[ForeignKey("TriggerUserId")]
	public virtual User? TriggerUser { get; set; }

	[ForeignKey("RelatedMangaId")]
	public virtual Manga? RelatedManga { get; set; }

	[ForeignKey("RelatedChapterId")]
	public virtual Chapter? RelatedChapter { get; set; }

	[ForeignKey("RelatedCommentId")]
	public virtual ChapterComment? RelatedComment { get; set; }
}

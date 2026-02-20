using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MangaReader.Models;

public class UserBookmark
{
	public int Id { get; set; }

	public int UserId { get; set; }

	public int MangaId { get; set; }

	public BookmarkStatus Status { get; set; }

	public DateTime AddedAt { get; set; }

	public DateTime UpdatedAt { get; set; }

	[ForeignKey("UserId")]
	public User? User { get; set; }

	[ForeignKey("MangaId")]
	public Manga? Manga { get; set; }
}

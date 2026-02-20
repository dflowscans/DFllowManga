using System;
using System.ComponentModel.DataAnnotations;

namespace MangaReader.Models;

public class PfpDecoration
{
	public int Id { get; set; }

	[Required]
	[StringLength(100)]
	public string Name { get; set; } = string.Empty;

	[Required]
	[StringLength(500)]
	public string ImageUrl { get; set; } = string.Empty;

	public int LevelRequirement { get; set; } = 1;

	public int Price { get; set; }

	public bool IsAnimated { get; set; }

	public DateTime CreatedAt { get; set; } = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public bool IsLocked { get; set; }
}

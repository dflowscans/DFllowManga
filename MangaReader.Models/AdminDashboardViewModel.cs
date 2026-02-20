using System.Collections.Generic;

namespace MangaReader.Models;

public class AdminDashboardViewModel
{
	public int TotalManga { get; set; }

	public int TotalChapters { get; set; }

	public int TotalPages { get; set; }

	public int TotalViews { get; set; }

	public int TotalUsers { get; set; }

	public int UniqueVisitors { get; set; }

	public int NewUsersLast7Days { get; set; }

	public double ActiveRate { get; set; }

	public double UserEngagement { get; set; }

	public List<Manga> RecentManga { get; set; } = new List<Manga>();

	public List<Chapter> RecentChapters { get; set; } = new List<Chapter>();

	public List<string> TrafficLabels { get; set; } = new List<string>();

	public List<int> TrafficViews { get; set; } = new List<int>();

	public List<int> TrafficVisitors { get; set; } = new List<int>();

	public List<string> PopularMangaLabels { get; set; } = new List<string>();

	public List<int> PopularMangaViews { get; set; } = new List<int>();

	public List<PopularChapterViewModel> PopularChapters { get; set; } = new List<PopularChapterViewModel>();

	public List<string> GenreLabels { get; set; } = new List<string>();

	public List<int> GenreCounts { get; set; } = new List<int>();

	public List<string> DeviceLabels { get; set; } = new List<string>();

	public List<int> DeviceCounts { get; set; } = new List<int>();
}

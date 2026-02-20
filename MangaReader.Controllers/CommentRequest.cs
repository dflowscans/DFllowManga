namespace MangaReader.Controllers;

public class CommentRequest
{
	public int ChapterId { get; set; }

	public string Content { get; set; } = string.Empty;

	public int ParentId { get; set; }
}

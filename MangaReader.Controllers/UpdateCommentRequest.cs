namespace MangaReader.Controllers;

public class UpdateCommentRequest
{
	public int CommentId { get; set; }

	public string Content { get; set; } = string.Empty;
}

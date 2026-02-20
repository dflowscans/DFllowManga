namespace MangaReader.Controllers;

public class ReactionRequest
{
    public int CommentId { get; set; }
    public bool IsLike { get; set; }
}

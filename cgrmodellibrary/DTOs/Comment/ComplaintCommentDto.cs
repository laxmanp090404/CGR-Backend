namespace cgrmodellibrary.DTOs.Comment;

public class ComplaintCommentDto
{
    public int CommentId { get; set; }
    public int ComplaintId { get; set; }
    public string CommentText { get; set; } = null!;
    public int CommentedBy { get; set; }
    public string CommentedByName { get; set; } = null!;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}

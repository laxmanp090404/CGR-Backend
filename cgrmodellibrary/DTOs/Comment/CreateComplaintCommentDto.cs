using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.Comment;

public class CreateComplaintCommentDto
{
    [Required]
    public string CommentText { get; set; } = null!;
    public bool IsInternal { get; set; }
}

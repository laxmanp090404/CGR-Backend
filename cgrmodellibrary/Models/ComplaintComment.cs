using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;
public partial class ComplaintComment
{
    public long CommentId { get; set; }

    public long ComplaintId { get; set; }

    public int CommentedBy { get; set; }

    public string CommentText { get; set; } = null!;

    public bool IsInternal { get; set; }

    public long? ParentCommentId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Employee CommentedByNavigation { get; set; } = null!;

    public virtual Complaint Complaint { get; set; } = null!;

    public virtual ICollection<ComplaintComment> InverseParentComment { get; set; } = new List<ComplaintComment>();

    public virtual ComplaintComment? ParentComment { get; set; }
}

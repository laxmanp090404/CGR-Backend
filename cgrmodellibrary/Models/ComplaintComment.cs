using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class ComplaintComment
{
    public int CommentId { get; set; }

    public int ComplaintId { get; set; }

    public string CommentText { get; set; } = null!;

    public int CommentedBy { get; set; }

    public bool IsInternal { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Employee CommentedByNavigation { get; set; } = null!;

    public virtual Complaint Complaint { get; set; } = null!;
}

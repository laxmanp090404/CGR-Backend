using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class ComplaintAttachment
{
    public int AttachmentId { get; set; }

    public int ComplaintId { get; set; }

    public string OriginalFileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    public int UploadedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Complaint Complaint { get; set; } = null!;

    public virtual Employee UploadedByNavigation { get; set; } = null!;
}

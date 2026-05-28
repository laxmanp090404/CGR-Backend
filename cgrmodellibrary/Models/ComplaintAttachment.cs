using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;
public partial class ComplaintAttachment
{
    public long AttachmentId { get; set; }

    public long ComplaintId { get; set; }

    public int UploadedBy { get; set; }

    public string FileName { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    public string StoragePath { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Complaint Complaint { get; set; } = null!;

    public virtual Employee UploadedByNavigation { get; set; } = null!;
}

namespace cgrmodellibrary.DTOs.Attachment;

public class ComplaintAttachmentDto
{
    public int AttachmentId { get; set; }
    public int ComplaintId { get; set; }
    public string OriginalFileName { get; set; } = null!;
    public string MimeType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public string FilePath { get; set; } = null!;
    public int UploadedBy { get; set; }
    public string UploadedByName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

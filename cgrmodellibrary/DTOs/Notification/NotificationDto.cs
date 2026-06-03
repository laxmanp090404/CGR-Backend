namespace cgrmodellibrary.DTOs.Notification;

public class NotificationDto
{
    public int NotificationId { get; set; }
    public short NotificationTypeId { get; set; }
    public string NotificationTypeName { get; set; } = null!;
    public int? ReferenceComplaintId { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

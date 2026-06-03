using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.Notification;

namespace cgrbussinesslogic.Interfaces;

public interface INotificationService
{
    Task<PagedResultDto<NotificationDto>> GetMyNotificationsAsync(int employeeId, bool? isRead, int page, int pageSize);
    Task<int> GetUnreadCountAsync(int employeeId);
    Task MarkAllAsReadAsync(int employeeId);
    Task MarkAsReadAsync(int notificationId, int employeeId);
    Task SendAsync(int employeeId, short notificationTypeId, string title, string message, int? referenceComplaintId = null);
}

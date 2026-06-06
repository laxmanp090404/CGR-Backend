using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.Notification;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<PagedResultDto<NotificationDto>> GetMyNotificationsAsync(int employeeId, bool? isRead, int page, int pageSize)
    {
        var (items, total) = await _notificationRepository.GetByEmployeeIdAsync(employeeId, isRead, page, pageSize);
        return new PagedResultDto<NotificationDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<int> GetUnreadCountAsync(int employeeId)
    {
        return await _notificationRepository.GetUnreadCountAsync(employeeId);
    }

    public async Task MarkAllAsReadAsync(int employeeId)
    {
        await _notificationRepository.MarkAllAsReadAsync(employeeId);
    }

    public async Task MarkAsReadAsync(int notificationId, int employeeId)
    {
        var notification = await _notificationRepository.Get(notificationId);
        if (notification.EmployeeId != employeeId)
            throw new ForbiddenException("You cannot mark another user's notification as read.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _notificationRepository.Update(notification, notificationId);
        }
    }

    public async Task SendAsync(int employeeId, short notificationTypeId, string title, string message, int? referenceComplaintId = null)
    {
        var notification = new Notification
        {
            EmployeeId = employeeId,
            NotificationTypeId = notificationTypeId,
            Title = title,
            Message = message,
            ReferenceComplaintId = referenceComplaintId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        await _notificationRepository.Create(notification);
    }

    private static NotificationDto MapToDto(Notification n) => new()
    {
        NotificationId = n.NotificationId,
        NotificationTypeId = n.NotificationTypeId,
        NotificationTypeName = n.NotificationType?.NotificationTypeName ?? string.Empty,
        ReferenceComplaintId = n.ReferenceComplaintId,
        Title = n.Title,
        Message = n.Message,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    };
}

using cgrmodellibrary.DTOs.Notification;

namespace cgrbussinesslogic.Interfaces;

public interface INotificationPusher
{
    Task PushAsync(int employeeId, NotificationDto notificationDto);
}

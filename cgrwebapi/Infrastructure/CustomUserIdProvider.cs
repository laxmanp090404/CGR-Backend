using Microsoft.AspNetCore.SignalR;

namespace cgrwebapi.Infrastructure;

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("employee_id")?.Value;
    }
}

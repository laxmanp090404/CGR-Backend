using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace cgrwebapi.Hubs;

[Authorize]
public class NotificationHub : Hub
{
}

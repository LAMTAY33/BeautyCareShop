using Microsoft.AspNetCore.SignalR;

namespace BeautyCareShop.Hubs;

public class SupportHub : Hub
{
    public async Task JoinSupport() => await Groups.AddToGroupAsync(Context.ConnectionId, "lpx-support");

    public async Task SendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        var sender = Context.User?.Identity?.IsAuthenticated == true
            ? Context.User.Identity.Name ?? "Khách"
            : "Khách vãng lai";
        await Clients.Group("lpx-support").SendAsync("ReceiveMessage", sender, message.Trim(), DateTimeOffset.Now.ToString("HH:mm"));
    }
}

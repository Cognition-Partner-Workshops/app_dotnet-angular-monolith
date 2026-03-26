using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OrderManager.Api.Hubs;

[Authorize]
public class CallHub : Hub
{
    private static readonly ConcurrentDictionary<int, string> UserConnections = new();

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            UserConnections[userId.Value] = Context.ConnectionId;
            await Clients.Others.SendAsync("UserOnline", userId.Value);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            UserConnections.TryRemove(userId.Value, out _);
            await Clients.Others.SendAsync("UserOffline", userId.Value);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendOffer(int targetUserId, string offer)
    {
        var callerId = GetUserId();
        if (!callerId.HasValue) return;

        if (UserConnections.TryGetValue(targetUserId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveOffer", callerId.Value, offer);
        }
        else
        {
            await Clients.Caller.SendAsync("CallFailed", targetUserId, "User is offline. Call has been queued.");
        }
    }

    public async Task SendAnswer(int targetUserId, string answer)
    {
        var answererId = GetUserId();
        if (!answererId.HasValue) return;

        if (UserConnections.TryGetValue(targetUserId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveAnswer", answererId.Value, answer);
        }
    }

    public async Task SendIceCandidate(int targetUserId, string candidate)
    {
        var senderId = GetUserId();
        if (!senderId.HasValue) return;

        if (UserConnections.TryGetValue(targetUserId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveIceCandidate", senderId.Value, candidate);
        }
    }

    public async Task DeclineCall(int callerId)
    {
        var declinerId = GetUserId();
        if (!declinerId.HasValue) return;

        if (UserConnections.TryGetValue(callerId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("CallDeclined", declinerId.Value);
        }
    }

    public async Task EndCall(int targetUserId)
    {
        var enderId = GetUserId();
        if (!enderId.HasValue) return;

        if (UserConnections.TryGetValue(targetUserId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("CallEnded", enderId.Value);
        }
    }

    public static bool IsUserOnline(int userId)
    {
        return UserConnections.ContainsKey(userId);
    }

    private int? GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var userId))
            return userId;
        return null;
    }
}

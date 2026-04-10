using Microsoft.AspNetCore.SignalR;

namespace RfpCopilot.Api.Hubs;

public class RfpProgressHub : Hub
{
    public async Task JoinRfpGroup(int rfpDocumentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"rfp-{rfpDocumentId}");
    }

    public async Task LeaveRfpGroup(int rfpDocumentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"rfp-{rfpDocumentId}");
    }
}

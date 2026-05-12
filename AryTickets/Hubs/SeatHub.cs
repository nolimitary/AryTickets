using Microsoft.AspNetCore.SignalR;

namespace AryTickets.Hubs
{
    public class SeatHub : Hub
    {
        public async Task JoinPerformance(int performanceId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"performance-{performanceId}");
        }

        public async Task LeavePerformance(int performanceId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"performance-{performanceId}");
        }
    }
}

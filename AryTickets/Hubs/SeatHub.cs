using Microsoft.AspNetCore.SignalR;

namespace AryTickets.Hubs
{
    public class SeatHub : Hub
    {
        public async Task JoinShowtime(int showtimeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"showtime-{showtimeId}");
        }

        public async Task LeaveShowtime(int showtimeId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"showtime-{showtimeId}");
        }
    }
}

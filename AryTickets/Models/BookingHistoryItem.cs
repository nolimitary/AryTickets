using System;

namespace AryTickets.Models
{
    public class BookingHistoryItem
    {
        public Booking Booking { get; set; } = default!;

        // Resolved from the joined Performance row when available; null for bookings
        // whose performance was deleted.
        public DateTime? ShowDateTime { get; set; }

        public bool IsPast => ShowDateTime.HasValue && ShowDateTime.Value <= DateTime.UtcNow;

        // Cancellable while the show is at least two hours away — gives the box
        // office time to release the seats without a last-minute scramble.
        public bool CanCancel =>
            ShowDateTime.HasValue && ShowDateTime.Value > DateTime.UtcNow.AddHours(2);
    }
}

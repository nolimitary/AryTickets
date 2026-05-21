namespace AryTickets.Models
{
    public enum SeatStatus { Available, Taken, Selected }

    public enum SeatTier { Balcony, Standard, Premium, Royal, RoyalBox }

    public class Seat
    {
        public string SeatNumber { get; set; } = string.Empty;
        public SeatStatus Status { get; set; }
        public decimal Price { get; set; } = 35.00m;

        // Layout metadata used by the arena SVG. All coordinates are in the
        // 0..900 / 0..620 viewBox space; the view scales them to fit any width.
        public SeatTier Tier { get; set; } = SeatTier.Standard;
        public double X { get; set; }
        public double Y { get; set; }
        public double Rotation { get; set; }    // degrees — so the seat "faces" the stage
    }
}

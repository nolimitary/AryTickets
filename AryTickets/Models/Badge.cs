namespace AryTickets.Models
{
    public class Badge
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconSvg { get; set; }
        public string GlowColor { get; set; }
        public bool IsUnlocked { get; set; }
    }
}
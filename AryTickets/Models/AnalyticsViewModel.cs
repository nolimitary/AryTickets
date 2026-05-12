using System.Collections.Generic;

namespace AryTickets.Models
{
    public class AnalyticsViewModel
    {
        public List<string> RevenueDates { get; set; } = new();
        public List<decimal> RevenueValues { get; set; } = new();
        public List<string> TopProductionNames { get; set; } = new();
        public List<int> TopProductionBookings { get; set; } = new();
        public List<string> DayNames { get; set; } = new();
        public List<int> DayCounts { get; set; } = new();
        public List<string> RevenueProductionNames { get; set; } = new();
        public List<decimal> RevenueProductionValues { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal TodayRevenue { get; set; }
    }
}

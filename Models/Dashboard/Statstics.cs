namespace BusTicketing.Models
{
    public class DashboardStats
    {
        public int TotalAgencies { get; set; }
        public int TotalCompanies { get; set; }
        public int TotalTickets { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Ticket> LatestTickets { get; set; } = new();
    }

    public class Ticket
    {
        public int Id { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime BookingDate { get; set; }
        public string AgencyName { get; set; } = string.Empty;
    }
}

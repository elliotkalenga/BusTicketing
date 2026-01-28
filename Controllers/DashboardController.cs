using BusTicketing.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace BusTicketing.Controllers
{
    [Auth]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            var model = new DashboardVm
            {
                TotalRoutes = 82,
                ActiveBuses = 118,
                TicketsSoldToday = 312,
                RevenueTodayCDF = 15840000m,
                PassengersToday = 298,

                RecentBookings = new List<RecentBookingVm>
                {
                    new("CDF-001021", "Jean Mukendi", "Kinshasa → Matadi", 55000, "8 mins ago"),
                    new("CDF-001022", "Chantal Nzambe", "Lubumbashi → Likasi", 42000, "15 mins ago"),
                    new("CDF-001023", "Patrick Kabeya", "Goma → Bukavu", 38000, "29 mins ago"),
                }
            };

            return View(model);
        }
    }

    // ================= VIEW MODELS =================

    public class DashboardVm
    {
        public int TotalRoutes { get; set; }
        public int ActiveBuses { get; set; }
        public int TicketsSoldToday { get; set; }
        public int PassengersToday { get; set; }
        public decimal RevenueTodayCDF { get; set; }
        public List<RecentBookingVm> RecentBookings { get; set; } = new();
    }

    public record RecentBookingVm(
        string TicketNo,
        string Passenger,
        string Route,
        decimal Amount,
        string Time
    );
}

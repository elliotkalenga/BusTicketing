using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BusTicketing.ViewModels
{
    public class TripVm
    {
        public int Id { get; set; }
        public int AgencyId { get; set; }

        public int RouteId { get; set; }
        public int BusId { get; set; }

        // 🔒 DISPLAY ONLY – NOT POSTED
        [BindNever]
        public string? RouteName { get; set; }

        [BindNever]
        public string? BusReg { get; set; }

        public DateTime DepartureTimeLocal { get; set; }
        public DateTime? ArrivalTimeLocal { get; set; }

        public int Status { get; set; }
    }
}

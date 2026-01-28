using BusTicketing.Models.Company;
using BusTicketing.Models.Location;

namespace BusTicketing.Models.Network
{
    public class RouteStop
    {
        public int Id { get; set; }

        public int RouteId { get; set; }
        public BusRoute BusRoute { get; set; } = default!;

        public int TerminalId { get; set; }
        public Area Area { get; set; } = default!;

        public int Sequence { get; set; }
        public int DwellMinutes { get; set; } = 10;
    }
}

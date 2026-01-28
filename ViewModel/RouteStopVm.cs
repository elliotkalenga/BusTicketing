namespace BusTicketing.ViewModels
{
    public class RouteStopVm
    {
        public int Id { get; set; }

        public int BusRouteId { get; set; }
        public string RouteName { get; set; } = "";

        public int TerminalId { get; set; }
        public string TerminalName { get; set; } = "";

        public int Sequence { get; set; }
        public int DwellMinutes { get; set; } = 10;
    }
}

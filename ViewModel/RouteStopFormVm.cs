namespace BusTicketing.ViewModels
{
    public class RouteStopFormVm
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public string RouteName { get; set; } = "";
        public int TerminalId { get; set; }
        public string TerminalName { get; set; } = "";
        public int Sequence { get; set; }
        public int DwellMinutes { get; set; } = 10;
    }
}

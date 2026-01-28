using BusTicketing.Models.Network;

public class TripStopTime
{
    public int Id { get; set; }

    public int TripId { get; set; }
    public Trip Trip { get; set; } = default!;

    public int RouteStopId { get; set; }
    public RouteStop RouteStop { get; set; } = default!; // replace BusStop

    public DateTime ArrivalTime { get; set; }
    public DateTime DepartureTime { get; set; }
}

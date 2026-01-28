namespace BusTicketing.Models.Fleet
{
    public class BusAmenityMap
    {
        public int BusId { get; set; }
        public Bus Bus { get; set; } = null!;

        public int BusAmenityId { get; set; }
        public BusAmenity BusAmenity { get; set; } = null!;
    }
}

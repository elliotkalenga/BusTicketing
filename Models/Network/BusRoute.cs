using System.ComponentModel.DataAnnotations;

namespace BusTicketing.Models.Network
{
    public class BusRoute
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        [Required] public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        [Required, MaxLength(128)] public string CreatedBy { get; set; } = default!;

        public ICollection<RouteStop> Stops { get; set; } = new List<RouteStop>();
    }
}

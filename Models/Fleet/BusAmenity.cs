
using System.ComponentModel.DataAnnotations;
using BusTicketing.Models.Common;

namespace BusTicketing.Models.Fleet
{
    public class BusAmenity
    {
        [Key] public int Id { get; set; }

        [Required, MaxLength(64)] public string Name { get; set; } = default!;
        public ICollection<BusAmenityMap> Buses { get; set; }
       = new List<BusAmenityMap>();
    }
}

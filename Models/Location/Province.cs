using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusTicketing.Models.Location
{
    public class Province
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; } // Optional (e.g. KIN, KAT)

        // Navigation
        public ICollection<Town> Towns { get; set; } = new List<Town>();
    }
}

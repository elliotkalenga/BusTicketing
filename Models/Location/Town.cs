using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketing.Models.Location
{
    public class Town
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Foreign Key
        [Required]
        public int ProvinceId { get; set; }

        // Navigation
        public Province Province { get; set; } = null!;
        public ICollection<Area> Areas { get; set; } = new List<Area>();
    }
}

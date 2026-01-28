using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketing.Models.Location
{
    public class Area
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Foreign Key
        [Required]
        public int TownId { get; set; }

        // Navigation
        public Town Town { get; set; } = null!;
    }
}

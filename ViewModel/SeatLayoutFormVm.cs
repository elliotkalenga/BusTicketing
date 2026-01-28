
using System.ComponentModel.DataAnnotations;

namespace BusTicketing.ViewModels
{
    public class SeatLayoutFormVm
    {
        public int Id { get; set; } // maps to dbo.SeatLayouts.Id

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Rows { get; set; }

        [Range(1, int.MaxValue)]
        public int Columns { get; set; }

        // Convenience display for tables
        public string Display => $"{Name} ({Rows}, {Columns})";
    }
}

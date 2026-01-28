
using System.ComponentModel.DataAnnotations;

namespace BusTicketing.ViewModels
{
    public class SeatDefinitionFormVm
    {
        public int Id { get; set; } // maps to dbo.SeatDefinitions.Id

        [Required]
        public int SeatLayoutId { get; set; }

        [Required, StringLength(8)]
        public string SeatNumber { get; set; } = string.Empty;

        public bool IsAisle { get; set; }

        [Range(0, int.MaxValue)]
        public int Class { get; set; } = 0; // e.g., 0=Economy, 1=Business, 2=VIP

        // Display-only convenience for list
        public string? SeatLayoutDisplay { get; set; }
    }
}

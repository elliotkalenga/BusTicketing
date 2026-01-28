
using System.ComponentModel.DataAnnotations;
using BusTicketing.Models.Common;

namespace BusTicketing.Models.Fleet
{
    public class SeatDefinition
    {
        [Key] public int Id { get; set; }

        [Required] public int SeatLayoutId { get; set; }
        public SeatLayout SeatLayout { get; set; } = default!;

        [Required, MaxLength(8)] public string SeatNumber { get; set; } = default!;
        public bool IsAisle { get; set; }
        public FareClass Class { get; set; } = FareClass.Standard;
    }
}

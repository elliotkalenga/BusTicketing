
using System.ComponentModel.DataAnnotations;
using BusTicketing.Models.Common;

namespace BusTicketing.Models.Sales
{
    public class BookingItem : AuditableBranchEntity
    {
        [Required] public int BookingId { get; set; }
        public Booking Booking { get; set; } = default!;

        [Required, MaxLength(8)] public string SeatNumber { get; set; } = default!;
        public FareClass Class { get; set; } = FareClass.Standard;

        public int? FromTerminalId { get; set; }
        public int? ToTerminalId { get; set; }

        public Money Price { get; set; } = new Money();
        public bool IsChild { get; set; }
        public bool IsSenior { get; set; }
    }
}

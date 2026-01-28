
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BusTicketing.Models.Common;
using BusTicketing.Models.Network;

namespace BusTicketing.Models.Sales
{
    public class Booking : AuditableBranchEntity
    {
        [Required] public int TripId { get; set; }
        public Trip Trip { get; set; } = default!;

        [Required] public int PassengerId { get; set; }
        public Passenger Passenger { get; set; } = default!;

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        [MaxLength(32)] public string Channel { get; set; } = "Web";
        [MaxLength(16)] public string? Pnr { get; set; }

        public ICollection<BookingItem> Items { get; set; } = new List<BookingItem>();
        public ICollection<BusTicketing.Models.Payment.Payment> Payments { get; set; } = new List<BusTicketing.Models.Payment.Payment>();
        public Money TotalAmount { get; set; } = new Money();
        public Money? DiscountAmount { get; set; }
        public Money? NetAmount { get; set; }
    }
}

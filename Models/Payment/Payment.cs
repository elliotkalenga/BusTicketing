
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BusTicketing.Models.Common;
using BusTicketing.Models.Sales;

namespace BusTicketing.Models.Payment
{
    public class Payment : AuditableBranchEntity
    {
        [Required] public int BookingId { get; set; }
        public Booking Booking { get; set; } = default!;

        public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;

        public Money Amount { get; set; } = new Money();
        [MaxLength(32)] public string Method { get; set; } = "MobileMoney";
        [MaxLength(64)] public string Provider { get; set; } = "Mpamba";
        [MaxLength(64)] public string ExternalReference { get; set; } = "";
        [MaxLength(64)] public string? AuthorizationCode { get; set; }

        public DateTime? AuthorizedAtUtc { get; set; }
        public DateTime? CapturedAtUtc { get; set; }
        public DateTime? SettledAtUtc { get; set; }

        public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    }
}

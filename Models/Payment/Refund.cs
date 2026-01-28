
using System;
using BusTicketing.Models.Common;

namespace BusTicketing.Models.Payment
{
    public class Refund : AuditableBranchEntity
    {
        public int PaymentId { get; set; }
        public Payment Payment { get; set; } = default!;

        public Money Amount { get; set; } = new Money();
        public string Reason { get; set; } = "";
        public PaymentStatus Status { get; set; } = PaymentStatus.Refunded;

        public string? ExternalReference { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
    }
}

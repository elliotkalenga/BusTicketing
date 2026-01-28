
using BusTicketing.Models.Common;
using System;

namespace BusTicketing.Models.Comms
{
    public class WebhookEvent : AuditableBranchEntity
    {
        public string Source { get; set; } = default!;   // PSP, SMS provider, etc.
        public string EventType { get; set; } = default!;
        public string PayloadJson { get; set; } = default!;
        public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;

        public int? PaymentId { get; set; }
    }
}

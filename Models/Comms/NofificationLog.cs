
using System;
using BusTicketing.Models.Common;

namespace BusTicketing.Comms
{
    public class NotificationLog : AuditableBranchEntity
    {
        public string Type { get; set; } = "Email"; // Email/SMS/Push
        public string Destination { get; set; } = default!;
        public string TemplateKey { get; set; } = default!;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

        public int? BookingId { get; set; }
        public int? TicketId { get; set; }
        public int? PaymentId { get; set; }
    }
}

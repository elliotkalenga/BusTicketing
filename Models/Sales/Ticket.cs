
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BusTicketing.Models.Common;
using BusTicketing.Models.Sales;

namespace BusTicketing.Sales
{
    public class Ticket : AuditableBranchEntity
    {
        [Required] public int BookingId { get; set; }
        public Booking Booking { get; set; } = default!;

        [Required, MaxLength(24)] public string TicketNumber { get; set; } = default!;
        public TicketStatus Status { get; set; } = TicketStatus.Issued;

        [MaxLength(256)] public string? QrCodeData { get; set; }
        public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<BoardingEvent> BoardingEvents { get; set; } = new List<BoardingEvent>();
    }
}

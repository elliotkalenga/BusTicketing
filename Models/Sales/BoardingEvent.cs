
using System;
using BusTicketing.Models.Common;
using BusTicketing.Models.Company;
using BusTicketing.Models.Location;
using BusTicketing.Sales;

namespace BusTicketing.Models.Sales
{
    public class BoardingEvent : AuditableBranchEntity
    {
        public int TicketId { get; set; }
        public Ticket Ticket { get; set; } = default!;

        public int TerminalId { get; set; }
        public Area Terminal { get; set; } = default!;

        public DateTime ScannedAtUtc { get; set; } = DateTime.UtcNow;
        public string DeviceId { get; set; } = default!;
        public bool Allowed { get; set; }
        public string? Reason { get; set; }
    }
}

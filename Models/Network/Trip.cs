using BusTicketing.Models.Common;
using BusTicketing.Models.Fleet;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusTicketing.Models.Network
{
    public class Trip
    {
        public int Id { get; set; }

        [Required]
        public int AgencyId { get; set; }

        [Required]
        public int RouteId { get; set; }
        public BusRoute BusRoute { get; set; } = default!;

        [Required]
        public int BusId { get; set; }
        public Bus Bus { get; set; } = default!;

        [Required]
        public DateTime DepartureTimeLocal { get; set; }
        public DateTime? ArrivalTimeLocal { get; set; }
        public TripStatus Status { get; set; } = TripStatus.Scheduled;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(128)]
        public string CreatedBy { get; set; } = default!;

        public ICollection<TripStopTime> StopTimes { get; set; } = new List<TripStopTime>();
        public ICollection<InventorySnapshot> InventorySnapshots { get; set; } = new List<InventorySnapshot>();
    }
}

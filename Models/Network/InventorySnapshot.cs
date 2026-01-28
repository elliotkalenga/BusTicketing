using BusTicketing.Models.Common;
using System;

namespace BusTicketing.Models.Network
{
    public class InventorySnapshot
    {
        public int Id { get; set; }
        public int AgencyId { get; set; }
        public int TripId { get; set; }
        public Trip Trip { get; set; } = default!;
        public int AvailableSeatsStandard { get; set; }
        public int AvailableSeatsPremium { get; set; }
        public int AvailableSeatsVip { get; set; }
        public Money FromPriceStandard { get; set; } = new();
        public Money FromPricePremium { get; set; } = new();
        public Money FromPriceVip { get; set; } = new();
        public DateTime SnapshotUtc { get; set; } = DateTime.UtcNow;
    }
}

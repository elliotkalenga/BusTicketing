
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BusTicketing.Models.Common;

namespace BusTicketing.Models.Payment
{
    public class SettlementBatch : AuditableBranchEntity
    {
        [Required, MaxLength(64)] public string Provider { get; set; } = default!;
        public DateTime PeriodStartUtc { get; set; }
        public DateTime PeriodEndUtc { get; set; }

        public Money TotalCaptured { get; set; } = new Money();
        public Money TotalRefunded { get; set; } = new Money();
        public Money NetAmount { get; set; } = new Money();

        public ICollection<SettlementItem> Items { get; set; } = new List<SettlementItem>();
        public bool Closed { get; set; }
    }
}

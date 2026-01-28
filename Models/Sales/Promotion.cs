
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BusTicketing.Models.Common;

namespace BusTicketing.Models.Sales
{
    public class Promotion : AuditableBranchEntity
    {
        [Required, MaxLength(32)] public string Code { get; set; } = default!;
        [MaxLength(256)] public string Description { get; set; } = "";

        [Column(TypeName = "decimal(5,2)")] public decimal? PercentOff { get; set; }
        public Money? AmountOff { get; set; }

        public DateTime ValidFromUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ValidToUtc { get; set; }
        public bool IsActive { get; set; } = true;

        public bool RestrictToCompany { get; set; }
        public int? RestrictedCompanyId { get; set; }
        public bool RestrictToRoute { get; set; }
        public int? RestrictedRouteId { get; set; }
    }
}

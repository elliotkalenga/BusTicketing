using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BusTicketing.Models.Common;

namespace BusTicketing.Models.Network
{
    public class FareRule
    {
        public int Id { get; set; }

        [Required]
        public int AgencyId { get; set; }

        [Required]
        public int RouteId { get; set; }
        public BusRoute Route { get; set; } = default!;

        [Required]
        public int FromTerminalId { get; set; }

        [Required]
        public int ToTerminalId { get; set; }

        public FareClass Class { get; set; } = FareClass.Standard;

        [Required]
        public Money BaseFare { get; set; } = new Money();

        [Column(TypeName = "decimal(5,2)")]
        public decimal? PercentageMarkup { get; set; }

        public Money? AbsoluteMarkup { get; set; }

        public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow.Date;
        public DateTime? EffectiveToUtc { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        [MaxLength(128)] public string CreatedBy { get; set; } = default!;

    }
}

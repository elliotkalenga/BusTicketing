
using BusTicketing.Models.Company;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketing.Models.Common
{
    /// <summary>Branch-scoped base with full auditing & optimistic concurrency.</summary>
    public abstract class AuditableBranchEntity 
    {
        [Key] public int Id { get; set; }

        public int? AgencyId { get; set; } // Nullable if role can exist without a branch

        [Required] public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        [Required, MaxLength(128)] public string CreatedBy { get; set; } = default!;
        public DateTime? UpdatedAtUtc { get; set; }
        [MaxLength(128)] public string? UpdatedBy { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        [MaxLength(128)] public string? DeletedBy { get; set; }
        public bool IsDeleted { get; set; }

    }
}


using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BusTicketing.Models.Common;

namespace BusTicketing.Models.Audit
{
    public class AuditLog : AuditableBranchEntity
    {
        [Required, MaxLength(128)] public string EntityName { get; set; } = default!;
        [Required] public int EntityId { get; set; }
        [Required, MaxLength(16)] public string Action { get; set; } = default!; // CREATE/UPDATE/DELETE
        [Required, MaxLength(128)] public string Actor { get; set; } = default!;
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<AuditPropertyChange> Changes { get; set; } = new List<AuditPropertyChange>();
    }
}

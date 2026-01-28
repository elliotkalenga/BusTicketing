
using System.ComponentModel.DataAnnotations;
using BusTicketing.Models.Common;

namespace BusTicketing.Models.Audit
{
    public class AuditPropertyChange : AuditableBranchEntity
    {
        [Required] public int AuditLogId { get; set; }
        public AuditLog AuditLog { get; set; } = default!;
        [Required, MaxLength(128)] public string PropertyName { get; set; } = default!;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}


using System.ComponentModel.DataAnnotations;
using BusTicketing.Models.Common;

namespace BusTicketing.Models.Staffing
{
    public class StaffProfile : AuditableBranchEntity
    {
        [Required, MaxLength(64)] public string Username { get; set; } = default!;
        [MaxLength(128)] public string? FullName { get; set; }
        [MaxLength(64)] public string Role { get; set; } = "Agent";
        [MaxLength(64)] public string? Phone { get; set; }
        [MaxLength(128)] public string? Email { get; set; }
        public bool Active { get; set; } = true;
    }
}

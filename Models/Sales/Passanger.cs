
using System.ComponentModel.DataAnnotations;
using BusTicketing.Models.Common;

namespace BusTicketing.Models.Sales
{
    public class Passenger : AuditableBranchEntity
    {
        [Required, MaxLength(128)] public string FullName { get; set; } = default!;
        [MaxLength(64)] public string? NationalId { get; set; }
        [MaxLength(64)] public string? PassportNumber { get; set; }
        [MaxLength(64)] public string Phone { get; set; } = default!;
        [MaxLength(128)] public string? Email { get; set; }
    }
}

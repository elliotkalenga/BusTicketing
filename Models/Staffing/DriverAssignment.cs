
using System.ComponentModel.DataAnnotations;
using BusTicketing.Models.Common;
using BusTicketing.Models.Network;

namespace BusTicketing.Models.Staffing
{
    public class DriverAssignment : AuditableBranchEntity
    {
        [Required] public int TripId { get; set; }
        public Trip Trip { get; set; } = default!;

        [Required] public int StaffProfileId { get; set; }
        public StaffProfile Driver { get; set; } = default!;
        public string? LicenseNumber { get; set; }
    }
}

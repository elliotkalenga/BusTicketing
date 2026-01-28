using BusTicketing.Models.Auth;
using BusTicketing.Models.Company;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketing.Models.Auth
{
    public class RolePermission
    {
        public int Id { get; set; }

        public int RoleId { get; set; }
        public Role? Role { get; set; }
        public int? AgencyId { get; set; } // Nullable if role can exist without a branch


        // Nav
        public int PermissionId { get; set; }
        public Permission? Permission { get; set; }
        public string? CreatedBy { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}

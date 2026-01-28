using BusTicketing.Models.Auth;
using BusTicketing.Models.Company;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace  BusTicketing.Models.Auth
{
    public class Role
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string? Name { get; set; }
        [NotMapped]
        public string? AgencyName { get; set; }
        [MaxLength(300)]
        public string? Description { get; set; }

        // Foreign key to Branch
        public int? AgencyId { get; set; } // Nullable if role can exist without a branch


        // Navigation
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusTicketing.Models.Auth
{
    public class Permission
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string? Code { get; set; }  

        [MaxLength(400)]
        public string? Description { get; set; }

        // Navigation (many-to-many via RolePermission)
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}

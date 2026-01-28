using BusTicketing.Models.Company;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketing.Models.Auth
{
    public class User
    {
        public int Id { get; set; }

        public int? AgencyId { get; set; } // Nullable if role can exist without a branch

        [NotMapped]
        public string? AgencyName { get; set; }

        [NotMapped]
        public string? TownName { get; set; }

        [NotMapped]
        public string? ProvinceName { get; set; }

        [NotMapped]
        public string? AreaName { get; set; }

        [Required, MaxLength(100)]
        public string? Username { get; set; }

        [Required, MaxLength(200)]
        public string? PasswordHash { get; set; }

        [MaxLength(200)]
        public string? FullName { get; set; }
        public int? AreaId { get; set; } = 0;

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }
        public string? CreatedBy { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}

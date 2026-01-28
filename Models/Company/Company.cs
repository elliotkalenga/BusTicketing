using BusTicketing.Models.Company;
using BusTicketing.Models.Location;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusTicketing.Models.Company
{
    public class Company
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string? Name { get; set; }

        [Required, MaxLength(100)]
        public string? RegistrationNumber { get; set; }

        [Required, MaxLength(500)]
        public string? Address { get; set; }

        [Required, MaxLength(50)]
        public string? Phone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Agency> Agencies { get; set; } = new List<Agency>();

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}

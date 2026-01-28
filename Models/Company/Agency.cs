using BusTicketing.Models.Company;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketing.Models.Company
{
    public class Agency
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }

        // FIX: Fully qualify the Company class to avoid namespace/type conflict
        public BusTicketing.Models.Company.Company Company { get; set; } = null!;

        [Required, MaxLength(150)]
        public string? Name { get; set; }

        [MaxLength(300)]
        public string? Location { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }


        [MaxLength(50)]
        public string? RegistrationNumber{ get; set; }


        [MaxLength(50)]
        public string? ServiceProCompany { get; set; }


        [MaxLength(50)]
        public string? Country { get; set; }


        [NotMapped]
        public string? CompanyName { get; set; }

    }
}

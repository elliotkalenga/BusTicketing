using BusTicketing.Models.Common;
using BusTicketing.Models.Company;
using Microsoft.VisualBasic.FileIO;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketing.Models.Fleet
{
    public class Bus : AuditableBranchEntity
    {
        // Registration number (Plate Number)
        [Required, MaxLength(32)]
        public string PlateNumber { get; set; } = default!;

        [Required, MaxLength(32)]
        public string ReferenceNumber { get; set; } = default!;

        // VIN or Chassis No.
        [Required, MaxLength(64)]
        public string ChassisNumber { get; set; } = default!;

        // Engine No.
        [MaxLength(64)]
        public string? EngineNumber { get; set; }

        // Manufacturing year
        [Range(1950, 2100)]
        public int YearOfMake { get; set; }

        // Brand + Model (e.g., Toyota Coaster)
        [MaxLength(64)]
        public string? MakeModel { get; set; }

        // Seating capacity
        [Range(1, 200)]
        public int Capacity { get; set; }

        // Bus fuel type
        public FuelType FuelType { get; set; }

        // Odometer value
        public int Mileage { get; set; }

        public DateTime RegistrationDate { get; set; }
        // Operation status
        public BusStatus Status { get; set; } = BusStatus.InService;

        // Optional seat layout
        public int? SeatLayoutId { get; set; }
        public SeatLayout? SeatLayout { get; set; }

        [NotMapped]
        public string? AgencyName { get; set; }
        public ICollection<BusAmenityMap> BusAmenities { get; set; }
       = new List<BusAmenityMap>();
    }
}

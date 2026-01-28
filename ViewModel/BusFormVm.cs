
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketing.ViewModels
{
    public class BusFormVm
    {
        public int BusId { get; set; }
        public string PlateNumber { get; set; } = "";
        public string ReferenceNumber { get; set; } = "";
        public string MakeModel { get; set; }
        public string ChassisNumber { get; set; } = "";
        public string EngineNumber { get; set; }
        public int Mileage { get; set; }
        public int YearOfMake { get; set; }
        public int Capacity { get; set; }
        public int FuelType { get; set; }
        public int Status { get; set; }
        public int? AgencyId { get; set; }
        public int? SeatLayoutId { get; set; }
        public string? SeatLayout { get; set; }
        public DateTime? RegistrationDate { get; set; } = DateTime.UtcNow;
        public string? AgencyName { get; set; }

        // New property to hold amenities
        public List<string> Amenities { get; set; } = new();
    }
}

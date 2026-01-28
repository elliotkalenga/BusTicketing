
using System.ComponentModel.DataAnnotations;

namespace BusTicketing.ViewModels
{
    public class BusAmenityFormVm
    {
        public int Id { get; set; }

        [Required, MaxLength(64)]
        public string Name { get; set; } = string.Empty;
    }
}


using System.ComponentModel.DataAnnotations;

namespace BusTicketing.ViewModels
{
    public class TerminalFormVm
    {
        public int Id { get; set; }

        [Required, StringLength(128)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(16)]
        public string Code { get; set; } = string.Empty;

        // Optional geocoordinates
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}

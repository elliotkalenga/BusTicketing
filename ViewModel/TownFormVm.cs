using System.ComponentModel.DataAnnotations;

namespace BusTicketing.ViewModels
{
    public class TownFormVm
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int ProvinceId { get; set; }

        // display only
        public string? ProvinceName { get; set; }
    }
}

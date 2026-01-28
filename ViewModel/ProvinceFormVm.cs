using System.ComponentModel.DataAnnotations;

namespace BusTicketing.ViewModels
{
    public class ProvinceFormVm
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Code { get; set; }
    }
}

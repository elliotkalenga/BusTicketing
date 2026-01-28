using System.ComponentModel.DataAnnotations;

namespace BusTicketing.ViewModels
{
    public class AreaFormVm
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int TownId { get; set; }

        // display only
        public string? TownName { get; set; }
    }
}

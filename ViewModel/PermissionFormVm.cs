
using System.ComponentModel.DataAnnotations;

namespace BusTicketing.ViewModels
{
    public class PermissionFormVm
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Code { get; set; } = string.Empty;

        [StringLength(400)]
        public string? Description { get; set; }
    }
}

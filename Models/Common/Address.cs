
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Models.Common
{
    [Owned]
    public class Address
    {
        [MaxLength(128)] public string Line1 { get; set; } = "";
        [MaxLength(128)] public string? Line2 { get; set; }
        [MaxLength(64)] public string City { get; set; } = "";
        [MaxLength(64)] public string State { get; set; } = "";
        [MaxLength(64)] public string Country { get; set; } = "Malawi";
        [MaxLength(16)] public string? PostalCode { get; set; }
    }
}

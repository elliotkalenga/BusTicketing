
using BusTicketing.Models.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusTicketing.Models.Fleet
{
    public class SeatLayout 
    {
        [Key] public int Id { get; set; }

        public string Name { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public ICollection<SeatDefinition> Seats { get; set; } = new List<SeatDefinition>();
    }
}

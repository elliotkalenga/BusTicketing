using BusTicketing.Filters;
using BusTicketing.Helpers;
using BusTicketing.Models.Fleet;
using BusTicketing.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BusTicketing.Controllers.Fleet
{
    [Auth]
    public class BusAmenitiesController : Controller
    {
        private readonly IConfiguration _config;

        public BusAmenitiesController(IConfiguration config)
        {
            _config = config;
        }

        // ------------------- INDEX ------------------------
        public IActionResult Index()
        {
            int? agencyId = HttpContext.Session.GetInt32("AgencyId");

            // Load buses
            var buses = new List<Bus>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            string busSql = "SELECT Id, PlateNumber, MakeModel, Capacity FROM Buses WHERE AgencyId=@AgencyId";
            using var cmd = new SqlCommand(busSql, conn);
            cmd.Parameters.AddWithValue("@AgencyId", agencyId ?? (object)DBNull.Value);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                buses.Add(new Bus
                {
                    Id = (int)reader["Id"],
                    PlateNumber = reader["PlateNumber"].ToString(),
                    MakeModel = reader["MakeModel"].ToString(),
                    Capacity = (int)reader["Capacity"]
                });
            }

            ViewBag.AllAmenities = LoadAmenities();
            ViewBag.BusAmenitiesMap = LoadBusAmenitiesMap();

            return View(buses);
        }

        // ------------------- SAVE ------------------------
        [HttpPost]
        public IActionResult Save(int BusId, int[] AmenityIds)
        {
            string? username = HttpContext.Session.GetString("UserName");

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Delete old mappings
                string deleteSql = "DELETE FROM BusAmenityMap WHERE BusId=@BusId";
                using var delCmd = new SqlCommand(deleteSql, conn);
                delCmd.Parameters.AddWithValue("@BusId", BusId);
                delCmd.ExecuteNonQuery();

                // Insert new mappings
                foreach (var aid in AmenityIds ?? Array.Empty<int>())
                {
                    string insertSql = @"INSERT INTO BusAmenityMap (BusId, BusAmenityId) 
                                         VALUES (@BusId, @BusAmenityId)";
                    using var cmd = new SqlCommand(insertSql, conn);
                    cmd.Parameters.AddWithValue("@BusId", BusId);
                    cmd.Parameters.AddWithValue("@BusAmenityId", aid);
                    cmd.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = "Bus amenities updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ------------------- HELPERS ------------------------
        private List<BusAmenity> LoadAmenities()
        {
            var list = new List<BusAmenity>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            string sql = "SELECT Id, Name FROM BusAmenities ORDER BY Name";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new BusAmenity
                {
                    Id = (int)reader["Id"],
                    Name = reader["Name"].ToString()
                });
            }
            return list;
        }

        private Dictionary<int, List<int>> LoadBusAmenitiesMap()
        {
            var map = new Dictionary<int, List<int>>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            string sql = "SELECT BusId, BusAmenityId FROM BusAmenityMap";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int busId = (int)reader["BusId"];
                int amenityId = (int)reader["BusAmenityId"];
                if (!map.ContainsKey(busId))
                    map[busId] = new List<int>();
                map[busId].Add(amenityId);
            }
            return map;
        }
    }
}

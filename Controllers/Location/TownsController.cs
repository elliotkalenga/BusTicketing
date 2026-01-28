using BusTicketing.Filters;
using BusTicketing.Helpers;
using BusTicketing.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;

namespace BusTicketing.Controllers
{
    [Auth]
    public class TownsController : Controller
    {
        private readonly IConfiguration _config;
        public TownsController(IConfiguration config) => _config = config;

        public IActionResult Index()
        {
            var list = new List<TownFormVm>();

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            const string sql = @"
SELECT T.Id, T.Name, T.ProvinceId, P.Name ProvinceName
FROM dbo.Towns T
INNER JOIN dbo.Provinces P ON P.Id = T.ProvinceId
ORDER BY T.Name";

            using var cmd = new SqlCommand(sql, conn);
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new TownFormVm
                {
                    Id = (int)r["Id"],
                    Name = r["Name"].ToString()!,
                    ProvinceId = (int)r["ProvinceId"],
                    ProvinceName = r["ProvinceName"].ToString()
                });
            }

            ViewBag.Provinces = GetProvinces();
            return PartialView("_Towns", list);
        }

        [HttpPost]
        public IActionResult Save(TownFormVm m)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var sql = m.Id == 0
                ? "INSERT INTO dbo.Towns(Name,ProvinceId) VALUES(@Name,@ProvinceId)"
                : "UPDATE dbo.Towns SET Name=@Name, ProvinceId=@ProvinceId WHERE Id=@Id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = m.Name;
            cmd.Parameters.Add("@ProvinceId", SqlDbType.Int).Value = m.ProvinceId;

            if (m.Id != 0)
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = m.Id;

            cmd.ExecuteNonQuery();
            TempData["SuccessMessage"] = m.Id == 0 ? "Town added successfully!" : "Town updated successfully!";
            return RedirectToAction("Index", "LocationSetup");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var sql = "DELETE FROM dbo.Towns WHERE Id=@Id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            cmd.ExecuteNonQuery();
            TempData["SuccessMessage"] = "Town deleted successfully!";
            // Redirect back to LocationSetup tab
            return RedirectToAction("Index", "LocationSetup");
        }

        private IEnumerable<dynamic> GetProvinces()
        {
            var list = new List<dynamic>();

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new SqlCommand(
                "SELECT Id, Name FROM dbo.Provinces ORDER BY Name", conn);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new { Id = (int)r["Id"], Name = r["Name"].ToString() });

            return list;
        }
    }
}

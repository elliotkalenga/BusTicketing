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
    public class AreasController : Controller
    {
        private readonly IConfiguration _config;
        public AreasController(IConfiguration config) => _config = config;

        public IActionResult Index()
        {
            var list = new List<AreaFormVm>();

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            const string sql = @"
SELECT A.Id, A.Name, A.TownId, T.Name TownName
FROM dbo.Areas A
INNER JOIN dbo.Towns T ON T.Id = A.TownId
ORDER BY A.Name";

            using var cmd = new SqlCommand(sql, conn);
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new AreaFormVm
                {
                    Id = (int)r["Id"],
                    Name = r["Name"].ToString()!,
                    TownId = (int)r["TownId"],
                    TownName = r["TownName"].ToString()
                });
            }

            ViewBag.Towns = GetTowns();
            return PartialView("_Areas", list);
        }

        [HttpPost]
        public IActionResult Save(AreaFormVm m)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var sql = m.Id == 0
                ? "INSERT INTO dbo.Areas(Name,TownId) VALUES(@Name,@TownId)"
                : "UPDATE dbo.Areas SET Name=@Name, TownId=@TownId WHERE Id=@Id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = m.Name;
            cmd.Parameters.Add("@TownId", SqlDbType.Int).Value = m.TownId;

            if (m.Id != 0)
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = m.Id;

            cmd.ExecuteNonQuery();
            TempData["SuccessMessage"] = m.Id == 0 ? "Area added successfully!" : "Area updated successfully!";
            return RedirectToAction("Index", "LocationSetup");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var sql = "DELETE FROM dbo.Areas WHERE Id=@Id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            cmd.ExecuteNonQuery();

            // Optionally, return JSON if using AJAX, or redirect
            TempData["SuccessMessage"] = "Area deleted successfully!";
            return RedirectToAction("Index", "LocationSetup");
        }

        private IEnumerable<dynamic> GetTowns()
        {
            var list = new List<dynamic>();

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new SqlCommand(
                "SELECT Id, Name FROM dbo.Towns ORDER BY Name", conn);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new { Id = (int)r["Id"], Name = r["Name"].ToString() });

            return list;
        }
    }
}

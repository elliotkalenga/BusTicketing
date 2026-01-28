using BusTicketing.Filters;
using BusTicketing.Helpers;
using BusTicketing.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BusTicketing.Controllers
{
    [Auth]
    public class ProvincesController : Controller
    {
        private readonly IConfiguration _config;
        public ProvincesController(IConfiguration config) => _config = config;

        public IActionResult Index()
        {
            var list = new List<ProvinceFormVm>();

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new SqlCommand(
                "SELECT Id, Name, Code FROM dbo.Provinces ORDER BY Name", conn);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new ProvinceFormVm
                {
                    Id = (int)r["Id"],
                    Name = r["Name"].ToString()!,
                    Code = r["Code"]?.ToString()
                });
            }

            return PartialView("_Provinces", list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(ProvinceFormVm m)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Index));

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var sql = m.Id == 0
                ? "INSERT INTO dbo.Provinces(Name,Code) VALUES(@Name,@Code)"
                : "UPDATE dbo.Provinces SET Name=@Name, Code=@Code WHERE Id=@Id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = m.Name;
            cmd.Parameters.Add("@Code", SqlDbType.NVarChar, 50).Value = (object?)m.Code ?? DBNull.Value;

            if (m.Id != 0)
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = m.Id;

            cmd.ExecuteNonQuery();
            TempData["SuccessMessage"] = m.Id == 0 ? "Province added successfully!" : "Province updated successfully!";
            return RedirectToAction("Index", "LocationSetup");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new SqlCommand(
                "DELETE FROM dbo.Provinces WHERE Id=@Id", conn);

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            cmd.ExecuteNonQuery();
            TempData["SuccessMessage"] = "Privince deleted successfully!";
            return RedirectToAction("Index", "LocationSetup");
        }
    }
}

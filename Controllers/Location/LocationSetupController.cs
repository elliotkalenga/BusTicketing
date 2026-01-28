using BusTicketing.Filters;
using BusTicketing.Helpers;
using BusTicketing.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BusTicketing.Controllers
{
    [Auth]
    public class LocationSetupController : Controller
    {
        private readonly IConfiguration _config;

        public LocationSetupController(IConfiguration config)
        {
            _config = config;
        }

        // ---------------- INDEX ----------------
        public IActionResult Index()
        {
            if (!HasPermission("Manage_Locations"))
                return RedirectToAction("Index", "AccessDenied");

            ViewBag.ProvincesModel = GetProvinces();
            ViewBag.TownsModel = GetTowns();
            ViewBag.AreasModel = GetAreas();

            return View();
        }

        // ---------------- SAVE PROVINCE ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveProvince(ProvinceFormVm model)
        {
            if (!HasPermission("Manage_Locations"))
                return RedirectToAction("Index", "AccessDenied");

            model.Name = model.Name?.Trim();
            model.Code = model.Code?.Trim();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Validation failed.";
                TempData["ShowProvinceModal"] = "true";
                return RedirectToAction("Index");
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                if (model.Id == 0)
                {
                    const string insertSql = "INSERT INTO Provinces (Name, Code) VALUES (@Name, @Code)";
                    using var cmd = new SqlCommand(insertSql, conn);
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = model.Name!;
                    cmd.Parameters.Add("@Code", SqlDbType.NVarChar, 20).Value = (object?)model.Code ?? DBNull.Value;
                    cmd.ExecuteNonQuery();

                    TempData["SuccessMessage"] = "Province created successfully!";
                }
                else
                {
                    const string updateSql = "UPDATE Provinces SET Name=@Name, Code=@Code WHERE Id=@Id";
                    using var cmd = new SqlCommand(updateSql, conn);
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = model.Name!;
                    cmd.Parameters.Add("@Code", SqlDbType.NVarChar, 20).Value = (object?)model.Code ?? DBNull.Value;
                    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;
                    cmd.ExecuteNonQuery();

                    TempData["SuccessMessage"] = "Province updated successfully!";
                }
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                TempData["ErrorMessage"] = "Province name already exists.";
                TempData["ShowProvinceModal"] = "true";
            }

            return RedirectToAction("Index");
        }

        // ---------------- DELETE PROVINCE ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProvince(int id)
        {
            if (!HasPermission("Manage_Locations"))
                return RedirectToAction("Index", "AccessDenied");

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new SqlCommand("DELETE FROM Provinces WHERE Id=@Id", conn);
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "Province deleted successfully!";
            return RedirectToAction("Index");
        }

        // ---------------- HELPERS ----------------
        private List<ProvinceFormVm> GetProvinces()
        {
            var list = new List<ProvinceFormVm>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            const string sql = "SELECT Id, Name, Code FROM Provinces ORDER BY Name";
            using var cmd = new SqlCommand(sql, conn);
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

            return list;
        }

        private List<TownFormVm> GetTowns()
        {
            var list = new List<TownFormVm>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            const string sql = @"
SELECT T.Id, T.Name, T.ProvinceId, P.Name AS ProvinceName
FROM Towns T
INNER JOIN Provinces P ON T.ProvinceId = P.Id
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
                    ProvinceName = r["ProvinceName"].ToString()!
                });
            }

            return list;
        }

        private List<AreaFormVm> GetAreas()
        {
            var list = new List<AreaFormVm>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            const string sql = @"
SELECT A.Id, A.Name, A.TownId, T.Name AS TownName
FROM Areas A
INNER JOIN Towns T ON A.TownId = T.Id
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
                    TownName = r["TownName"].ToString()!
                });
            }

            return list;
        }

        // ---------------- PERMISSION CHECK ----------------
        private bool HasPermission(string permission)
        {
            var perms = HttpContext.Session.GetString("Permissions");
            return perms?.Split(',').Contains(permission) == true;
        }
    }
}

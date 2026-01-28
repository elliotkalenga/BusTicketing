using BusTicketing.Filters;
using BusTicketing.Helpers;
using BusTicketing.Models.Network;
using BusTicketing.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BusTicketing.Controllers
{
    [Auth]
    public class BusRouteController : Controller
    {
        private readonly IConfiguration _config;
        public BusRouteController(IConfiguration config) => _config = config;

        // ---------- INDEX ----------
        public IActionResult Index()
        {
            if (!HasPermission("Manage_Bus_Routes"))
                return RedirectToAction("Index", "AccessDenied");

            var list = new List<BusRouteVm>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            const string sql = @"SELECT Id, Name FROM dbo.BusRoutes ORDER BY Name;";
            using (var cmd = new SqlCommand(sql, conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new BusRouteVm
                    {
                        Id = Convert.ToInt32(r["Id"]),
                        Name = r["Name"]?.ToString() ?? ""
                    });
                }
            }

            return View(list);
        }

        // ---------- SAVE ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save([FromForm] BusRouteVm model)
        {
            if (!HasPermission("Manage_Bus_Routes"))
                return RedirectToAction("Index", "AccessDenied");

            var username = HttpContext.Session.GetString("Username") ?? "system";

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["ErrorMessage"] = "Route name is required.";
                PreserveForm(model);
                TempData["ShowBusRouteModal"] = "true";
                return RedirectToAction("Index");
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                if (model.Id == 0)
                {
                    const string insertSql = @"INSERT INTO dbo.BusRoutes (Name, CreatedAtUtc, CreatedBy) VALUES (@Name, SYSUTCDATETIME(), @CreatedBy); SELECT CAST(SCOPE_IDENTITY() AS int);";
                    using var cmd = new SqlCommand(insertSql, conn);
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 128).Value = model.Name;
                    cmd.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 128).Value = username;
                    var newId = Convert.ToInt32(cmd.ExecuteScalar());
                    TempData["SuccessMessage"] = $"Bus Route #{newId} created successfully!";
                }
                else
                {
                    const string updateSql = @"UPDATE dbo.BusRoutes SET Name=@Name WHERE Id=@Id;";
                    using var cmd = new SqlCommand(updateSql, conn);
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 128).Value = model.Name;
                    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;
                    var rows = cmd.ExecuteNonQuery();
                    TempData["SuccessMessage"] = rows > 0 ? $"Bus Route #{model.Id} updated successfully!" : "Route not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unexpected error: " + ex.Message;
                TempData["ShowBusRouteModal"] = "true";
                PreserveForm(model);
            }

            return RedirectToAction("Index");
        }

        // ---------- DELETE ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!HasPermission("Manage_Bus_Routes"))
                return RedirectToAction("Index", "AccessDenied");

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                const string sql = "DELETE FROM dbo.BusRoutes WHERE Id=@Id;";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                var rows = cmd.ExecuteNonQuery();
                TempData["SuccessMessage"] = rows > 0 ? "Bus Route deleted successfully!" : "Route not found.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unexpected error: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ---------- Helpers ----------
        private void PreserveForm(BusRouteVm model)
        {
            TempData["Form.Id"] = model.Id;
            TempData["Form.Name"] = model.Name ?? "";
        }

        private bool HasPermission(string permission)
        {
            var perms = HttpContext.Session.GetString("Permissions");
            return !string.IsNullOrEmpty(perms) && perms.Split(',').Contains(permission);
        }
    }
}

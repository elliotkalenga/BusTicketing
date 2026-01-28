using BusTicketing.Filters;
using BusTicketing.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

// Alias to avoid conflict between namespace and class
using CompanyModel = BusTicketing.Models.Company.Company;

namespace BusTicketing.Controllers
{
    [Auth]
    public class CompanyController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IStringLocalizer<BusTicketing.AppResource> _localizer;

        public CompanyController(IConfiguration config, IStringLocalizer<BusTicketing.AppResource> localizer)
        {
            _config = config;
            _localizer = localizer;
        }

        public IActionResult Index()
        {
            if (!HasPermission("View_Companies"))
            {
                return RedirectToAction("Index", "AccessDenied");
            }

            var companies = new List<CompanyModel>();

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var sql = @"
                    SELECT Id, Name, RegistrationNumber, Address, Phone, CreatedAt, RowVersion
                    FROM Companies
                    ORDER BY Id DESC";

                using var cmd = new SqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    companies.Add(new CompanyModel
                    {
                        Id = (int)reader["Id"],
                        Name = reader["Name"]?.ToString(),
                        RegistrationNumber = reader["RegistrationNumber"]?.ToString(),
                        Address = reader["Address"]?.ToString(),
                        Phone = reader["Phone"]?.ToString(),
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        RowVersion = reader["RowVersion"] as byte[]
                    });
                }
            }

            return View(companies);
        }

        [HttpPost]
        public IActionResult Save(CompanyModel model)
        {
            if (!HasPermission("Update_Companies"))
            {
                return RedirectToAction("Index", "AccessDenied");
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                if (model.Id == 0)
                {
                    var sql = @"
                        INSERT INTO Companies (Name, RegistrationNumber, Address, Phone, CreatedAt)
                        VALUES (@Name, @RegistrationNumber, @Address, @Phone, SYSUTCDATETIME());";

                    using var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
                    cmd.Parameters.AddWithValue("@RegistrationNumber", model.RegistrationNumber ?? "");
                    cmd.Parameters.AddWithValue("@Address", model.Address ?? "");
                    cmd.Parameters.AddWithValue("@Phone", model.Phone ?? "");
                    cmd.ExecuteNonQuery();

                    TempData["SuccessMessage"] = _localizer["CompanyCreated"].Value;
                }
                else
                {
                    var sql = @"
                        UPDATE Companies
                        SET Name=@Name, RegistrationNumber=@RegistrationNumber, Address=@Address, Phone=@Phone
                        WHERE Id=@Id AND RowVersion=@RowVersion;

                        SELECT @@ROWCOUNT;";

                    using var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Id", model.Id);
                    cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
                    cmd.Parameters.AddWithValue("@RegistrationNumber", model.RegistrationNumber ?? "");
                    cmd.Parameters.AddWithValue("@Address", model.Address ?? "");
                    cmd.Parameters.AddWithValue("@Phone", model.Phone ?? "");
                    cmd.Parameters.AddWithValue("@RowVersion", (object?)model.RowVersion ?? DBNull.Value);

                    var affectedObj = cmd.ExecuteScalar();
                    var affected = affectedObj is int i ? i : Convert.ToInt32(affectedObj ?? 0);

                    if (affected == 0)
                    {
                        TempData["ErrorMessage"] = _localizer["ConcurrencyError"].Value;
                        return RedirectToAction("Index");
                    }

                    TempData["SuccessMessage"] = _localizer["CompanyUpdated"].Value;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = _localizer["ErrorOccurred"].Value + ": " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            if (!HasPermission("Delete_Companies"))
            {
                return RedirectToAction("Index", "AccessDenied");
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                var sql = "DELETE FROM Companies WHERE Id=@Id";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();

                TempData["SuccessMessage"] = _localizer["CompanyDeleted"].Value;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = _localizer["ErrorOccurred"].Value + ": " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        private bool HasPermission(string permission)
        {
            var perms = HttpContext.Session.GetString("Permissions");
            if (string.IsNullOrEmpty(perms)) return false;

            var list = perms.Split(',');
            return list.Contains(permission);
        }
    }
}

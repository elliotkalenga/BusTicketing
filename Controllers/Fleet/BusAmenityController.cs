using BusTicketing.Filters;
using BusTicketing.Helpers;
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
    public class BusAmenityController : Controller
    {
        private readonly IConfiguration _config;
        public BusAmenityController(IConfiguration config) => _config = config;

        // ---------- INDEX ----------
        public IActionResult Index()
        {
            if (!HasPermission("Manage_Bus_Amenities"))
                return RedirectToAction("Index", "AccessDenied");

            var list = new List<BusAmenityFormVm>();

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            const string sql = @"
SELECT Id, Name
FROM dbo.BusAmenities
ORDER BY Name;";

            using var cmd = new SqlCommand(sql, conn);
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new BusAmenityFormVm
                {
                    Id = (int)r["Id"],
                    Name = r["Name"]?.ToString() ?? string.Empty
                });
            }

            return View(list);
        }

        // ---------- SAVE (ADD / EDIT) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(BusAmenityFormVm model)
        {
            if (!HasPermission("Manage_Bus_Amenities"))
                return RedirectToAction("Index", "AccessDenied");

            model.Name = model.Name?.Trim() ?? "";

            // ----- Validation -----
            if (string.IsNullOrWhiteSpace(model.Name))
                ModelState.AddModelError(nameof(model.Name), "Amenity name is required.");

            // ----- Duplicate check -----
            if (ModelState.IsValid)
            {
                try
                {
                    using var connDup = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                    connDup.Open();

                    const string dupSql = @"
SELECT COUNT(1)
FROM dbo.BusAmenities
WHERE Name = @Name
  AND (@Id = 0 OR Id <> @Id);";

                    using var dupCmd = new SqlCommand(dupSql, connDup);
                    dupCmd.Parameters.Add("@Name", SqlDbType.NVarChar, 64).Value = model.Name;
                    dupCmd.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;

                    if (Convert.ToInt32(dupCmd.ExecuteScalar()) > 0)
                        ModelState.AddModelError(nameof(model.Name), "An amenity with this name already exists.");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Duplicate check failed: " + ex.Message);
                }
            }

            if (!ModelState.IsValid)
            {
                PreserveForm(model);
                TempData["ErrorMessage"] = "Validation failed.";
                TempData["ErrorDetailsHtml"] = BuildErrorDetailsHtml(ModelState);
                TempData["ShowAmenityModal"] = "true";
                return RedirectToAction("Index");
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                if (model.Id == 0)
                {
                    const string insertSql = @"
INSERT INTO dbo.BusAmenities (Name)
VALUES (@Name);";

                    using var cmd = new SqlCommand(insertSql, conn);
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 64).Value = model.Name;
                    cmd.ExecuteNonQuery();

                    TempData["SuccessMessage"] = "Amenity created successfully!";
                }
                else
                {
                    const string updateSql = @"
UPDATE dbo.BusAmenities
SET Name = @Name
WHERE Id = @Id;";

                    using var cmd = new SqlCommand(updateSql, conn);
                    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 64).Value = model.Name;

                    var rows = cmd.ExecuteNonQuery();
                    TempData["SuccessMessage"] = rows == 0
                        ? $"No amenity found with Id = {model.Id}."
                        : "Amenity updated successfully!";
                }
            }
            catch (SqlException ex)
            {
                PreserveForm(model);
                TempData["ErrorMessage"] = $"SQL Error ({ex.Number})";
                TempData["ErrorDetailsHtml"] =
                    $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
                TempData["ShowAmenityModal"] = "true";
            }
            catch (Exception ex)
            {
                PreserveForm(model);
                TempData["ErrorMessage"] = "Unexpected error.";
                TempData["ErrorDetailsHtml"] =
                    $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
                TempData["ShowAmenityModal"] = "true";
            }

            return RedirectToAction("Index");
        }

        // ---------- DELETE ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!HasPermission("Manage_Bus_Amenities"))
                return RedirectToAction("Index", "AccessDenied");

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                const string sql = "DELETE FROM dbo.BusAmenities WHERE Id = @Id;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                var rows = cmd.ExecuteNonQuery();
                TempData["SuccessMessage"] = rows == 0
                    ? $"No amenity found with Id = {id}."
                    : "Amenity deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Delete failed.";
                TempData["ErrorDetailsHtml"] =
                    $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
            }

            return RedirectToAction("Index");
        }

        // ---------- HELPERS ----------
        private void PreserveForm(BusAmenityFormVm m)
        {
            TempData["Form.Id"] = m.Id;
            TempData["Form.Name"] = m.Name ?? "";
        }

        private static string BuildErrorDetailsHtml(
            Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
        {
            var sb = new StringBuilder("<ul class='mb-0'>");

            foreach (var kvp in modelState)
            {
                var key = string.IsNullOrWhiteSpace(kvp.Key) ? "General" : kvp.Key;
                foreach (var err in kvp.Value.Errors)
                {
                    if (!string.IsNullOrWhiteSpace(err.ErrorMessage))
                    {
                        sb.Append("<li><strong>")
                          .Append(System.Net.WebUtility.HtmlEncode(key))
                          .Append("</strong>: ")
                          .Append(System.Net.WebUtility.HtmlEncode(err.ErrorMessage))
                          .Append("</li>");
                    }
                }
            }

            sb.Append("</ul>");
            return sb.ToString();
        }

        private bool HasPermission(string permission)
        {
            var perms = HttpContext.Session.GetString("Permissions");
            return !string.IsNullOrEmpty(perms)
                && perms.Split(',').Contains(permission);
        }
    }
}

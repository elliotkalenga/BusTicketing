using BusTicketing.Filters;
using BusTicketing.Helpers;
using BusTicketing.ViewModels;
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
    public class SeatLayoutController : Controller
    {
        private readonly IConfiguration _config;
        public SeatLayoutController(IConfiguration config) => _config = config;

        // ---------- INDEX ----------
        public IActionResult Index()
        {
            if (!HasPermission("Manage_Seat_SeatLayouts"))
                return RedirectToAction("Index", "AccessDenied");

            var list = new List<SeatLayoutFormVm>();

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            const string sql = @"
SELECT Id, Name, Rows, Columns
FROM dbo.SeatLayouts
ORDER BY Name;";

            using var cmd = new SqlCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new SeatLayoutFormVm
                {
                    Id = Convert.ToInt32(r["Id"]),
                    Name = r["Name"]?.ToString() ?? "",
                    Rows = r["Rows"] == DBNull.Value ? 0 : Convert.ToInt32(r["Rows"]),
                    Columns = r["Columns"] == DBNull.Value ? 0 : Convert.ToInt32(r["Columns"])
                });
            }

            return View(list);
        }

        // ---------- SAVE ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save([FromForm] SeatLayoutFormVm model)
        {
            if (!HasPermission("Manage_Seat_SeatLayouts"))
                return RedirectToAction("Index", "AccessDenied");

            // Basic validations
            if (string.IsNullOrWhiteSpace(model.Name))
                ModelState.AddModelError(nameof(model.Name), "Layout name is required.");
            if (model.Rows <= 0)
                ModelState.AddModelError(nameof(model.Rows), "Rows must be 1 or more.");
            if (model.Columns <= 0)
                ModelState.AddModelError(nameof(model.Columns), "Columns must be 1 or more.");

            if (!ModelState.IsValid)
            {
                PreserveForm(model);
                TempData["ErrorMessage"] = "Validation failed. See details below.";
                TempData["ErrorDetailsHtml"] = BuildErrorDetailsHtml(ModelState);
                TempData["ShowLayoutModal"] = "true";
                return RedirectToAction("Index");
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                if (model.Id == 0)
                {
                    const string insertSql = @"
INSERT INTO dbo.SeatLayouts (Name, Rows, Columns)
VALUES (@Name, @Rows, @Columns);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                    using var cmd = new SqlCommand(insertSql, conn);
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, -1).Value = model.Name?.Trim() ?? "";
                    cmd.Parameters.Add("@Rows", SqlDbType.Int).Value = model.Rows;
                    cmd.Parameters.Add("@Columns", SqlDbType.Int).Value = model.Columns;

                    var newIdObj = cmd.ExecuteScalar();
                    var newId = (newIdObj is int i) ? i : Convert.ToInt32(newIdObj);
                    TempData["SuccessMessage"] = $"Seat layout #{newId} created successfully!";
                }
                else
                {
                    const string updateSql = @"
UPDATE dbo.SeatLayouts
SET Name = @Name,
    Rows = @Rows,
    Columns = @Columns
WHERE Id = @Id;";

                    using var cmd = new SqlCommand(updateSql, conn);
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, -1).Value = model.Name?.Trim() ?? "";
                    cmd.Parameters.Add("@Rows", SqlDbType.Int).Value = model.Rows;
                    cmd.Parameters.Add("@Columns", SqlDbType.Int).Value = model.Columns;
                    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;

                    var rows = cmd.ExecuteNonQuery();
                    TempData["SuccessMessage"] = rows == 0
                        ? $"No layout found with Id = {model.Id}."
                        : $"Seat layout #{model.Id} updated successfully!";
                }
            }
            catch (Exception ex)
            {
                PreserveForm(model);
                TempData["ErrorMessage"] = "Unexpected Error: " + ex.Message;
                TempData["ErrorDetailsHtml"] = $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
                TempData["ShowLayoutModal"] = "true";
            }

            return RedirectToAction("Index");
        }

        // ---------- DELETE ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!HasPermission("Manage_Seat_SeatLayouts"))
                return RedirectToAction("Index", "AccessDenied");

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                const string sql = "DELETE FROM dbo.SeatLayouts WHERE Id = @Id;";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                var rows = cmd.ExecuteNonQuery();
                TempData["SuccessMessage"] = rows == 0
                    ? $"No layout found with Id = {id}."
                    : "Seat layout deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unexpected Error: " + ex.Message;
                TempData["ErrorDetailsHtml"] = $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
            }

            return RedirectToAction("Index");
        }

        // ---------- Helpers ----------
        private void PreserveForm(SeatLayoutFormVm m)
        {
            TempData["Form.Id"] = m.Id;
            TempData["Form.Name"] = m.Name ?? "";
            TempData["Form.Rows"] = m.Rows;
            TempData["Form.Columns"] = m.Columns;
        }

        private static string BuildErrorDetailsHtml(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
        {
            var sb = new StringBuilder("<ul class='mb-0'>");

            foreach (var kvp in modelState)
            {
                var key = string.IsNullOrWhiteSpace(kvp.Key) ? "General" : kvp.Key;

                foreach (var err in kvp.Value.Errors)
                {
                    var msg = System.Net.WebUtility.HtmlEncode(err.ErrorMessage ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        sb.Append("<li><strong>")
                          .Append(System.Net.WebUtility.HtmlEncode(key))
                          .Append("</strong>: ")
                          .Append(msg)
                          .Append("</li>");
                    }
                }

                if (kvp.Value.Errors.Any(e => string.IsNullOrWhiteSpace(e.ErrorMessage)))
                {
                    sb.Append("<li><strong>")
                      .Append(System.Net.WebUtility.HtmlEncode(key))
                      .Append("</strong>: Invalid value.</li>");
                }
            }

            sb.Append("</ul>");
            return sb.ToString();
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

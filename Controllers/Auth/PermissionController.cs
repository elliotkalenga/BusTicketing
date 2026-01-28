
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
    public class PermissionController : Controller
    {
        private readonly IConfiguration _config;
        public PermissionController(IConfiguration config) => _config = config;

        // ---------- INDEX ----------
        public IActionResult Index()
        {
            if (!HasPermission("ManagePermissions"))
                return RedirectToAction("Index", "AccessDenied");

            var list = new List<PermissionFormVm>();

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            const string sql = @"SELECT Id, Code, Description FROM dbo.Permissions ORDER BY Code;";

            using var cmd = new SqlCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new PermissionFormVm
                {
                    Id = Convert.ToInt32(r["Id"]),
                    Code = r["Code"]?.ToString() ?? "",
                    Description = r["Description"] == DBNull.Value ? null : r["Description"]?.ToString()
                });
            }

            return View(list);
        }

        // ---------- SAVE ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save([FromForm] PermissionFormVm model)
        {
            if (!HasPermission("ManagePermissions"))
                return RedirectToAction("Index", "AccessDenied");

            if (string.IsNullOrWhiteSpace(model.Code))
                ModelState.AddModelError(nameof(model.Code), "Permission code is required.");

            if (!ModelState.IsValid)
            {
                PreserveForm(model);
                TempData["ErrorMessage"] = "Validation failed. See details below.";
                TempData["ErrorDetailsHtml"] = BuildErrorDetailsHtml(ModelState);
                TempData["ShowPermissionModal"] = "true";
                return RedirectToAction("Index");
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                if (model.Id == 0)
                {
                    const string insertSql = @"
INSERT INTO dbo.Permissions (Code, Description)
VALUES (@Code, @Description);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                    using var cmd = new SqlCommand(insertSql, conn);
                    cmd.Parameters.Add("@Code", SqlDbType.NVarChar, 120).Value = model.Code.Trim();
                    cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 400).Value = (object?)model.Description ?? DBNull.Value;

                    var newIdObj = cmd.ExecuteScalar();
                    var newId = (newIdObj is int i) ? i : Convert.ToInt32(newIdObj);
                    TempData["SuccessMessage"] = $"Permission #{newId} created successfully!";
                }
                else
                {
                    const string updateSql = @"
UPDATE dbo.Permissions
SET Code = @Code,
    Description = @Description
WHERE Id = @Id;";

                    using var cmd = new SqlCommand(updateSql, conn);
                    cmd.Parameters.Add("@Code", SqlDbType.NVarChar, 120).Value = model.Code.Trim();
                    cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 400).Value = (object?)model.Description ?? DBNull.Value;
                    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;

                    var rows = cmd.ExecuteNonQuery();
                    TempData["SuccessMessage"] = rows == 0
                        ? $"No permission found with Id = {model.Id}."
                        : $"Permission #{model.Id} updated successfully!";
                }
            }
            catch (SqlException ex)
            {
                PreserveForm(model);
                TempData["ErrorMessage"] = $"SQL Error ({ex.Number}): {ex.Message}";
                TempData["ErrorDetailsHtml"] = $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
                TempData["ShowPermissionModal"] = "true";
            }
            catch (Exception ex)
            {
                PreserveForm(model);
                TempData["ErrorMessage"] = "Unexpected Error: " + ex.Message;
                TempData["ErrorDetailsHtml"] = $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
                TempData["ShowPermissionModal"] = "true";
            }

            return RedirectToAction("Index");
        }

        // ---------- DELETE ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!HasPermission("ManagePermissions"))
                return RedirectToAction("Index", "AccessDenied");

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                const string sql = @"DELETE FROM dbo.Permissions WHERE Id = @Id;";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                var rows = cmd.ExecuteNonQuery();
                TempData["SuccessMessage"] = rows == 0
                    ? $"No permission found with Id = {id}."
                    : "Permission deleted successfully!";
            }
            catch (SqlException ex)
            {
                TempData["ErrorMessage"] = $"SQL Error ({ex.Number}): {ex.Message}";
                TempData["ErrorDetailsHtml"] = $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unexpected Error: " + ex.Message;
                TempData["ErrorDetailsHtml"] = $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
            }

            return RedirectToAction("Index");
        }

        // ---------- Helpers ----------
        private void PreserveForm(PermissionFormVm m)
        {
            TempData["Form.Id"] = m.Id;
            TempData["Form.Code"] = m.Code ?? "";
            TempData["Form.Description"] = m.Description ?? "";
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

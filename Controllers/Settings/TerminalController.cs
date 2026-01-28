
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
    public class TerminalController : Controller
    {
        private readonly IConfiguration _config;
        public TerminalController(IConfiguration config) => _config = config;

        // ---------- INDEX ----------
        public IActionResult Index()
        {
            if (!HasPermission("Manage_Bus_Terminals"))
                return RedirectToAction("Index", "AccessDenied");

            var list = new List<TerminalFormVm>();
            int? agencyId = HttpContext.Session.GetInt32("AgencyId");

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            const string sql = @"
SELECT Id, Name, Code, Latitude, Longitude
FROM dbo.Terminals
WHERE IsDeleted = 0
  AND (@AgencyId IS NULL OR AgencyId = @AgencyId)
ORDER BY Name, Code;";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@AgencyId", SqlDbType.Int).Value = (object?)agencyId ?? DBNull.Value;

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new TerminalFormVm
                    {
                        Id = Convert.ToInt32(r["Id"]),
                        Name = r["Name"]?.ToString() ?? "",
                        Code = r["Code"]?.ToString() ?? "",
                        Latitude = r["Latitude"] == DBNull.Value ? (double?)null : Convert.ToDouble(r["Latitude"]),
                        Longitude = r["Longitude"] == DBNull.Value ? (double?)null : Convert.ToDouble(r["Longitude"])
                    });
                }
            }

            return View(list);
        }

        // ---------- SAVE (ADD/EDIT via modal) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save([FromForm] TerminalFormVm model)
        {
            if (!HasPermission("Manage_Bus_Terminals"))
                return RedirectToAction("Index", "AccessDenied");

            // Tenancy context (NOT NULLs in table)
            int? branchId = HttpContext.Session.GetInt32("BranchId");
            int? companyId = HttpContext.Session.GetInt32("CompanyId");
            var username = HttpContext.Session.GetString("Username") ?? "system";

            if (companyId is null)
                ModelState.AddModelError("", "Company context is missing. Please select/assign a company.");
            if (branchId is null)
                ModelState.AddModelError("", "Branch context is missing. Please select/assign a branch.");

            // Basic validations
            if (string.IsNullOrWhiteSpace(model.Name))
                ModelState.AddModelError(nameof(model.Name), "Terminal name is required.");
            else if (model.Name.Length > 128)
                ModelState.AddModelError(nameof(model.Name), "Terminal name must be 128 characters or less.");

            if (string.IsNullOrWhiteSpace(model.Code))
                ModelState.AddModelError(nameof(model.Code), "Terminal code is required.");
            else if (model.Code.Length > 16)
                ModelState.AddModelError(nameof(model.Code), "Terminal code must be 16 characters or less.");

            // Optional: validate latitude/longitude ranges if provided
            if (model.Latitude.HasValue && (model.Latitude < -90 || model.Latitude > 90))
                ModelState.AddModelError(nameof(model.Latitude), "Latitude must be between -90 and 90.");
            if (model.Longitude.HasValue && (model.Longitude < -180 || model.Longitude > 180))
                ModelState.AddModelError(nameof(model.Longitude), "Longitude must be between -180 and 180.");

            // Enforce Code uniqueness within Company + Branch for active records
            if (companyId.HasValue && branchId.HasValue && !string.IsNullOrWhiteSpace(model.Code))
            {
                try
                {
                    using var connDup = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                    connDup.Open();
                    const string dupSql = @"
SELECT COUNT(1)
FROM dbo.Terminals
WHERE IsDeleted = 0
  AND CompanyId = @CompanyId
  AND AgencyId  = @agencyId
  AND Code = @Code
  AND (@Id = 0 OR Id <> @Id);";
                    using var dupCmd = new SqlCommand(dupSql, connDup);
                    dupCmd.Parameters.Add("@AgencyId", SqlDbType.Int).Value = companyId.Value;
                    dupCmd.Parameters.Add("@AgencyId", SqlDbType.Int).Value = branchId.Value;
                    dupCmd.Parameters.Add("@Code", SqlDbType.NVarChar, 16).Value = model.Code.Trim();
                    dupCmd.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;
                    var dup = Convert.ToInt32(dupCmd.ExecuteScalar());
                    if (dup > 0)
                        ModelState.AddModelError(nameof(model.Code), "A terminal with this code already exists in this branch.");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Duplicate check error: {ex.Message}");
                }
            }

            if (!ModelState.IsValid)
            {
                PreserveForm(model);
                TempData["ErrorMessage"] = "Validation failed. See details below.";
                TempData["ErrorDetailsHtml"] = BuildErrorDetailsHtml(ModelState);
                TempData["ShowTerminalModal"] = "true";
                return RedirectToAction("Index");
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                if (model.Id == 0)
                {
                    const string insertSql = @"
INSERT INTO dbo.Terminals
  (Name, Code, Latitude, Longitude,
   CompanyId, BranchId,
   CreatedAtUtc, CreatedBy,
   UpdatedAtUtc, UpdatedBy,
   DeletedAtUtc, DeletedBy, IsDeleted)
VALUES
  (@Name, @Code, @Latitude, @Longitude,
   @CompanyId, @BranchId,
   SYSUTCDATETIME(), @CreatedBy,
   NULL, NULL,
   NULL, NULL, 0);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                    using var cmd = new SqlCommand(insertSql, conn);
                    AddCommonParameters(cmd, model, companyId!.Value, branchId!.Value);
                    cmd.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 128).Value = username;

                    var newIdObj = cmd.ExecuteScalar();
                    var newId = (newIdObj is int i) ? i : Convert.ToInt32(newIdObj);
                    TempData["SuccessMessage"] = $"Terminal #{newId} created successfully!";
                }
                else
                {
                    const string updateSql = @"
UPDATE dbo.Terminals
SET Name         = @Name,
    Code         = @Code,
    Latitude     = @Latitude,
    Longitude    = @Longitude,
    CompanyId    = @CompanyId,
    BranchId     = @BranchId,
    UpdatedBy    = @UpdatedBy,
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE Id = @Id;";

                    using var cmd = new SqlCommand(updateSql, conn);
                    AddCommonParameters(cmd, model, companyId!.Value, branchId!.Value);
                    cmd.Parameters.Add("@UpdatedBy", SqlDbType.NVarChar, 128).Value = username;
                    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;

                    var rows = cmd.ExecuteNonQuery();
                    TempData["SuccessMessage"] = rows == 0
                        ? $"No terminal found with Id = {model.Id}."
                        : $"Terminal #{model.Id} updated successfully!";
                }
            }
            catch (SqlException ex)
            {
                PreserveForm(model);
                TempData["ErrorMessage"] = $"SQL Error ({ex.Number}): {ex.Message}";
                TempData["ErrorDetailsHtml"] = $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
                TempData["ShowTerminalModal"] = "true";
            }
            catch (Exception ex)
            {
                PreserveForm(model);
                TempData["ErrorMessage"] = "Unexpected Error: " + ex.Message;
                TempData["ErrorDetailsHtml"] = $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
                TempData["ShowTerminalModal"] = "true";
            }

            return RedirectToAction("Index");
        }

        // ---------- DELETE (Soft) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!HasPermission("Manage_Bus_Terminals"))
                return RedirectToAction("Index", "AccessDenied");

            try
            {
                var username = HttpContext.Session.GetString("Username") ?? "system";

                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                const string sql = @"
UPDATE dbo.Terminals
SET IsDeleted    = 1,
    DeletedBy    = @DeletedBy,
    DeletedAtUtc = SYSUTCDATETIME()
WHERE Id = @Id;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                cmd.Parameters.Add("@DeletedBy", SqlDbType.NVarChar, 128).Value = username;

                var rows = cmd.ExecuteNonQuery();
                TempData["SuccessMessage"] = rows == 0
                    ? $"No terminal found with Id = {id}."
                    : "Terminal deleted successfully!";
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
        private void PreserveForm(TerminalFormVm m)
        {
            TempData["Form.Id"] = m.Id;
            TempData["Form.Name"] = m.Name ?? "";
            TempData["Form.Code"] = m.Code ?? "";
            TempData["Form.Latitude"] = m.Latitude?.ToString() ?? "";
            TempData["Form.Longitude"] = m.Longitude?.ToString() ?? "";
        }

        private static void AddCommonParameters(SqlCommand cmd, TerminalFormVm m, int companyId, int branchId)
        {
            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 128).Value = m.Name?.Trim() ?? "";
            cmd.Parameters.Add("@Code", SqlDbType.NVarChar, 16).Value = m.Code?.Trim() ?? "";

            cmd.Parameters.Add("@Latitude", SqlDbType.Float).Value = (object?)m.Latitude ?? DBNull.Value;
            cmd.Parameters.Add("@Longitude", SqlDbType.Float).Value = (object?)m.Longitude ?? DBNull.Value;

            cmd.Parameters.Add("@CompanyId", SqlDbType.Int).Value = companyId;
            cmd.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;
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

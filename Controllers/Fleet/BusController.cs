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
    public class BusController : Controller
    {
        private readonly IConfiguration _config;
        public BusController(IConfiguration config) => _config = config;

        // ---------- INDEX ----------
        public IActionResult Index()
        {
            if (!HasPermission("View_Buses"))
                return RedirectToAction("Index", "AccessDenied");

            var list = new List<BusFormVm>();
            int? AgencyId = HttpContext.Session.GetInt32("AgencyId");

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            const string sql = @"
SELECT 
    B.Id,
    B.PlateNumber,
    B.ReferenceNumber,
    B.MakeModel,
    B.ChassisNumber,
    B.EngineNumber,
    B.Mileage,
    B.YearOfMake,
    B.Capacity,
    B.FuelType,
    B.Status,ag.Name as AgencyName,
    B.SeatLayoutId,
    B.AgencyId,
    B.RegistrationDate,
    S.Name As Seatlayout,
    ISNULL((
        SELECT STUFF((
            SELECT ', ' + BA.Name
            FROM dbo.BusAmenityMap BAM
            INNER JOIN dbo.BusAmenities BA ON BAM.BusAmenityId = BA.Id
            WHERE BAM.BusId = B.Id
            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '')
    ), '') AS Amenities
FROM dbo.Buses AS B 
INNER JOIN SeatLayouts S ON B.SeatLayoutId = S.Id
INNER JOIN Agencies ag on B.AgencyId=ag.id
WHERE B.IsDeleted = 0
  AND (@AgencyId IS NULL OR B.AgencyId = @AgencyId)
ORDER BY B.Id DESC;
";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@AgencyId", SqlDbType.Int).Value = (object?)AgencyId ?? DBNull.Value;

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new BusFormVm
                    {
                        BusId = r["Id"] != DBNull.Value ? Convert.ToInt32(r["Id"]) : 0,
                        PlateNumber = r["PlateNumber"]?.ToString() ?? "",
                        ReferenceNumber = r["ReferenceNumber"]?.ToString() ?? "",
                        MakeModel = r["MakeModel"]?.ToString() ?? "",
                        ChassisNumber = r["ChassisNumber"]?.ToString() ?? "",
                        EngineNumber = r["EngineNumber"]?.ToString() ?? "",
                        SeatLayout = r["Seatlayout"]?.ToString() ?? "",
                        AgencyName = r["AgencyName"]?.ToString() ?? "",
                        Mileage = r["Mileage"] != DBNull.Value ? Convert.ToInt32(r["Mileage"]) : 0,
                        YearOfMake = r["YearOfMake"] != DBNull.Value ? Convert.ToInt32(r["YearOfMake"]) : DateTime.UtcNow.Year,
                        Capacity = r["Capacity"] != DBNull.Value ? Convert.ToInt32(r["Capacity"]) : 1,
                        FuelType = r["FuelType"] != DBNull.Value ? Convert.ToInt32(r["FuelType"]) : 0,
                        Status = r["Status"] != DBNull.Value ? Convert.ToInt32(r["Status"]) : 0,
                        SeatLayoutId = r["SeatLayoutId"] != DBNull.Value ? Convert.ToInt32(r["SeatLayoutId"]) : (int?)null,
                        AgencyId = r["AgencyId"] != DBNull.Value ? Convert.ToInt32(r["AgencyId"]) : (int?)null,
                        RegistrationDate = r["RegistrationDate"] != DBNull.Value ? Convert.ToDateTime(r["RegistrationDate"]) : DateTime.UtcNow,
                        Amenities = (r["Amenities"]?.ToString() ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .ToList()
                    });
                }
            }

            // Dropdowns
            ViewBag.FuelTypes = GetFuelTypes();
            ViewBag.StatusOptions = GetStatusOptions();
            ViewBag.YearOptions = GetYearOptions(1990, DateTime.UtcNow.Year);
            ViewBag.SeatLayouts = GetSeatLayouts(conn).ToList();

            return View(list);
        }

        // ---------- SAVE ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save([FromForm] BusFormVm model)
        {
            if (!HasPermission("Create_Buses"))
                return RedirectToAction("Index", "AccessDenied");

            int? AgencyId = HttpContext.Session.GetInt32("AgencyId");
            var username = HttpContext.Session.GetString("Username") ?? "system";

            if (AgencyId is null)
            {
                ModelState.AddModelError("", "Agency context is missing. Please select/assign an Agency.");
            }

            if (!ModelState.IsValid)
            {
                PreserveForm(model);
                TempData["ErrorMessage"] = "Validation failed. See details below.";
                TempData["ErrorDetailsHtml"] = BuildErrorDetailsHtml(ModelState);
                TempData["ShowBusModal"] = "true";
                return RedirectToAction("Index");
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                if (model.BusId == 0)
                {
                    const string insertSql = @"
INSERT INTO dbo.Buses
(PlateNumber, ReferenceNumber, ChassisNumber, EngineNumber, YearOfMake, MakeModel,
 Capacity, FuelType, Mileage, Status, SeatLayoutId, AgencyId, IsDeleted,
 RegistrationDate, CreatedAtUtc, CreatedBy)
VALUES
(@PlateNumber, @ReferenceNumber, @ChassisNumber, @EngineNumber, @YearOfMake, @MakeModel,
 @Capacity, @FuelType, @Mileage, @Status, @SeatLayoutId, @AgencyId, 0,
 @RegistrationDate, SYSUTCDATETIME(), @CreatedBy);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                    using var cmd = new SqlCommand(insertSql, conn);
                    AddCommonParameters(cmd, model, AgencyId.Value);
                    cmd.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 128).Value = username;

                    var newIdObj = cmd.ExecuteScalar();
                    var newId = (newIdObj is int i) ? i : Convert.ToInt32(newIdObj);
                    TempData["SuccessMessage"] = $"Bus #{newId} created successfully!";
                }
                else
                {
                    const string updateSql = @"
UPDATE dbo.Buses
SET PlateNumber = @PlateNumber,
    ReferenceNumber = @ReferenceNumber,
    ChassisNumber = @ChassisNumber,
    EngineNumber = @EngineNumber,
    YearOfMake = @YearOfMake,
    MakeModel = @MakeModel,
    Capacity = @Capacity,
    FuelType = @FuelType,
    Mileage = @Mileage,
    Status = @Status,
    SeatLayoutId = @SeatLayoutId,
    AgencyId = @AgencyId,
    RegistrationDate = @RegistrationDate,
    UpdatedAtUtc = SYSUTCDATETIME(),
    UpdatedBy = @UpdatedBy
WHERE Id = @BusId;";

                    using var cmd = new SqlCommand(updateSql, conn);
                    AddCommonParameters(cmd, model, AgencyId.Value);
                    cmd.Parameters.Add("@UpdatedBy", SqlDbType.NVarChar, 128).Value = username;
                    cmd.Parameters.Add("@BusId", SqlDbType.Int).Value = model.BusId;

                    var rows = cmd.ExecuteNonQuery();
                    TempData["SuccessMessage"] = rows == 0
                        ? $"No bus found with BusId = {model.BusId}."
                        : $"Bus #{model.BusId} updated successfully!";
                }
            }
            catch (Exception ex)
            {
                PreserveForm(model);
                TempData["ErrorMessage"] = "Unexpected Error: " + ex.Message;
                TempData["ErrorDetailsHtml"] = $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
                TempData["ShowBusModal"] = "true";
            }

            return RedirectToAction("Index");
        }

        // ---------- DELETE ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int busId)
        {
            if (!HasPermission("Delete_Buses"))
                return RedirectToAction("Index", "AccessDenied");

            try
            {
                var username = HttpContext.Session.GetString("Username") ?? "system";

                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                const string sql = @"
UPDATE dbo.Buses
SET IsDeleted = 1,
    DeletedBy = @DeletedBy,
    DeletedAtUtc = SYSUTCDATETIME()
WHERE Id = @BusId;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@BusId", SqlDbType.Int).Value = busId;
                cmd.Parameters.Add("@DeletedBy", SqlDbType.NVarChar, 128).Value = username;

                var rows = cmd.ExecuteNonQuery();
                TempData["SuccessMessage"] = rows == 0
                    ? $"No bus found with BusId = {busId}."
                    : "Bus deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unexpected Error: " + ex.Message;
                TempData["ErrorDetailsHtml"] = $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
            }

            return RedirectToAction("Index");
        }

        // ---------- Helpers ----------
        private void PreserveForm(BusFormVm model)
        {
            TempData["Form.BusId"] = model.BusId;
            TempData["Form.PlateNumber"] = model.PlateNumber ?? "";
            TempData["Form.ReferenceNumber"] = model.ReferenceNumber ?? "";
            TempData["Form.ChassisNumber"] = model.ChassisNumber ?? "";
            TempData["Form.EngineNumber"] = model.EngineNumber ?? "";
            TempData["Form.MakeModel"] = model.MakeModel ?? "";
            TempData["Form.Mileage"] = model.Mileage;
            TempData["Form.YearOfMake"] = model.YearOfMake;
            TempData["Form.Capacity"] = model.Capacity;
            TempData["Form.FuelType"] = model.FuelType;
            TempData["Form.Status"] = model.Status;
            TempData["Form.SeatLayoutId"] = model.SeatLayoutId ?? 0;
            TempData["Form.RegistrationDate"] = model.RegistrationDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        private static void AddCommonParameters(SqlCommand cmd, BusFormVm m, int agencyId)
        {
            cmd.Parameters.Add("@PlateNumber", SqlDbType.NVarChar, 32).Value = m.PlateNumber ?? "";
            cmd.Parameters.Add("@ReferenceNumber", SqlDbType.NVarChar, 32).Value = m.ReferenceNumber ?? "";
            cmd.Parameters.Add("@ChassisNumber", SqlDbType.NVarChar, 64).Value = m.ChassisNumber ?? "";
            cmd.Parameters.Add("@EngineNumber", SqlDbType.NVarChar, 64).Value = (object?)m.EngineNumber ?? DBNull.Value;
            cmd.Parameters.Add("@YearOfMake", SqlDbType.Int).Value = m.YearOfMake;
            cmd.Parameters.Add("@MakeModel", SqlDbType.NVarChar, 64).Value = (object?)m.MakeModel ?? DBNull.Value;
            cmd.Parameters.Add("@Capacity", SqlDbType.Int).Value = m.Capacity;
            cmd.Parameters.Add("@FuelType", SqlDbType.Int).Value = m.FuelType;
            cmd.Parameters.Add("@Mileage", SqlDbType.Int).Value = m.Mileage;
            cmd.Parameters.Add("@Status", SqlDbType.Int).Value = m.Status;
            cmd.Parameters.Add("@SeatLayoutId", SqlDbType.Int).Value = (object?)m.SeatLayoutId ?? DBNull.Value;
            cmd.Parameters.Add("@AgencyId", SqlDbType.Int).Value = agencyId;
            cmd.Parameters.Add("@RegistrationDate", SqlDbType.Date).Value = (object?)m.RegistrationDate ?? DBNull.Value;
        }

        private static IEnumerable<(int value, string text)> GetFuelTypes()
        {
            yield return (0, "Diesel");
            yield return (1, "Petrol");
            yield return (2, "Electric");
            yield return (3, "Hybrid");
        }

        private static IEnumerable<(int value, string text)> GetStatusOptions()
        {
            yield return (0, "In Service");
            yield return (1, "Maintenance");
            yield return (2, "Inactive");
        }

        private static IEnumerable<int> GetYearOptions(int startYearInclusive, int endYearInclusive)
        {
            for (int y = endYearInclusive; y >= startYearInclusive; y--)
                yield return y;
        }

        private IEnumerable<dynamic> GetSeatLayouts(SqlConnection conn)
        {
            const string sql = @"SELECT Id, Name, Rows, Columns FROM dbo.SeatLayouts ORDER BY Name;";
            using var cmd = new SqlCommand(sql, conn);
            using var r = cmd.ExecuteReader();

            var result = new List<dynamic>();
            while (r.Read())
            {
                var id = r["Id"] != DBNull.Value ? Convert.ToInt32(r["Id"]) : 0;
                var name = r["Name"]?.ToString() ?? "";
                var rows = r["Rows"] != DBNull.Value ? Convert.ToInt32(r["Rows"]) : 0;
                var cols = r["Columns"] != DBNull.Value ? Convert.ToInt32(r["Columns"]) : 0;
                result.Add(new { Id = id, Display = $"{name} ({rows}, {cols})" });
            }
            return result;
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
                        sb.Append("<li><strong>").Append(System.Net.WebUtility.HtmlEncode(key)).Append("</strong>: ").Append(msg).Append("</li>");
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

using BusTicketing.Filters;
using BusTicketing.Helpers;
using BusTicketing.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    public class TripsController : Controller
    {
        private readonly IConfiguration _config;
        private const int CURRENT_AGENCY_ID = 1;

        public TripsController(IConfiguration config)
        {
            _config = config;
        }

        // ================= INDEX =================
        public IActionResult Index()
        {
            if (!HasPermission("Manage_Trips"))
                return RedirectToAction("Index", "AccessDenied");

            var trips = new List<TripVm>();

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            const string sql = @"
                SELECT  T.Id,
                        T.AgencyId,
                        T.RouteId,
                        R.Name AS RouteName,
                        T.BusId,
                        B.PlateNumber,
                        T.DepartureTimeLocal,
                        T.ArrivalTimeLocal,
                        T.Status
                FROM dbo.Trips T
                INNER JOIN dbo.BusRoutes R ON R.Id = T.RouteId
                INNER JOIN dbo.Buses B ON B.Id = T.BusId
                WHERE T.AgencyId = @AgencyId
                ORDER BY T.DepartureTimeLocal DESC;
            ";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@AgencyId", SqlDbType.Int).Value = CURRENT_AGENCY_ID;

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    trips.Add(new TripVm
                    {
                        Id = (int)r["Id"],
                        AgencyId = (int)r["AgencyId"],
                        RouteId = (int)r["RouteId"],
                        RouteName = r["RouteName"].ToString(),
                        BusId = (int)r["BusId"],
                        BusReg = r["PlateNumber"].ToString(),
                        DepartureTimeLocal = (DateTime)r["DepartureTimeLocal"],
                        ArrivalTimeLocal = r["ArrivalTimeLocal"] == DBNull.Value
                            ? null
                            : (DateTime?)r["ArrivalTimeLocal"],
                        Status = (int)r["Status"]
                    });
                }
            }

            ViewBag.Routes = GetRoutes(conn).ToList();
            ViewBag.Buses = GetActiveBuses(conn).ToList();

            return View(trips);
        }

        // ================= SAVE =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(TripVm model)
        {
            if (!HasPermission("Manage_Trips"))
                return RedirectToAction("Index", "AccessDenied");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the highlighted errors.";
                TempData["ErrorDetailsHtml"] = BuildErrorDetailsHtml(ModelState);
                TempData["ShowTripModal"] = "true";
                PreserveForm(model);
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                if (model.Id == 0)
                {
                    const string insertSql = @"
                        INSERT INTO dbo.Trips
                        (AgencyId, RouteId, BusId, DepartureTimeLocal, ArrivalTimeLocal, Status, CreatedAtUtc, CreatedBy)
                        VALUES
                        (@AgencyId, @RouteId, @BusId, @Departure, @Arrival, @Status, GETUTCDATE(), @User);
                    ";

                    using var cmd = new SqlCommand(insertSql, conn);
                    cmd.Parameters.AddWithValue("@AgencyId", CURRENT_AGENCY_ID);
                    cmd.Parameters.AddWithValue("@RouteId", model.RouteId);
                    cmd.Parameters.AddWithValue("@BusId", model.BusId);
                    cmd.Parameters.AddWithValue("@Departure", model.DepartureTimeLocal);
                    cmd.Parameters.AddWithValue("@Arrival", (object?)model.ArrivalTimeLocal ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Status", model.Status);
                    cmd.Parameters.AddWithValue("@User", User.Identity?.Name ?? "SYSTEM");

                    cmd.ExecuteNonQuery();
                    TempData["SuccessMessage"] = "Trip created successfully!";
                }
                else
                {
                    const string updateSql = @"
                        UPDATE dbo.Trips
                        SET RouteId = @RouteId,
                            BusId = @BusId,
                            DepartureTimeLocal = @Departure,
                            ArrivalTimeLocal = @Arrival,
                            Status = @Status
                        WHERE Id = @Id;
                    ";

                    using var cmd = new SqlCommand(updateSql, conn);
                    cmd.Parameters.AddWithValue("@RouteId", model.RouteId);
                    cmd.Parameters.AddWithValue("@BusId", model.BusId);
                    cmd.Parameters.AddWithValue("@Departure", model.DepartureTimeLocal);
                    cmd.Parameters.AddWithValue("@Arrival", (object?)model.ArrivalTimeLocal ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Status", model.Status);
                    cmd.Parameters.AddWithValue("@Id", model.Id);

                    cmd.ExecuteNonQuery();
                    TempData["SuccessMessage"] = "Trip updated successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unexpected system error.";
                TempData["ErrorDetailsHtml"] = $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
                TempData["ShowTripModal"] = "true";
                PreserveForm(model);
            }

            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new SqlCommand("DELETE FROM dbo.Trips WHERE Id=@Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "Trip deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ================= HELPERS =================

        private static IEnumerable<(int Id, string Name)> GetRoutes(SqlConnection conn)
        {
            using var cmd = new SqlCommand(
                "SELECT Id, Name FROM dbo.BusRoutes ORDER BY Name", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                yield return ((int)r["Id"], r["Name"].ToString());
        }

        private static IEnumerable<(int Id, string Reg)> GetActiveBuses(SqlConnection conn)
        {
            using var cmd = new SqlCommand(@"
                SELECT Id, PlateNumber
                FROM dbo.Buses
                WHERE IsDeleted = 0
                  AND Status = 1
                  AND AgencyId = @AgencyId
                ORDER BY PlateNumber", conn);

            cmd.Parameters.AddWithValue("@AgencyId", CURRENT_AGENCY_ID);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                yield return ((int)r["Id"], r["PlateNumber"].ToString());
        }

        private void PreserveForm(TripVm m)
        {
            TempData["Form.Id"] = m.Id;
            TempData["Form.RouteId"] = m.RouteId;
            TempData["Form.BusId"] = m.BusId;
            TempData["Form.Departure"] = m.DepartureTimeLocal.ToString("yyyy-MM-ddTHH:mm");
            TempData["Form.Arrival"] = m.ArrivalTimeLocal?.ToString("yyyy-MM-ddTHH:mm");
            TempData["Form.Status"] = m.Status;
        }

        private static string BuildErrorDetailsHtml(ModelStateDictionary ms)
        {
            var sb = new StringBuilder("<ul>");
            foreach (var k in ms)
                foreach (var e in k.Value.Errors)
                    sb.Append("<li>").Append(e.ErrorMessage).Append("</li>");
            return sb.Append("</ul>").ToString();
        }

        private bool HasPermission(string p)
        {
            var perms = HttpContext.Session.GetString("Permissions");
            return !string.IsNullOrEmpty(perms) && perms.Split(',').Contains(p);
        }
    }
}

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
    public class RouteStopController : Controller
    {
        private readonly IConfiguration _config;
        public RouteStopController(IConfiguration config) => _config = config;

        // ---------- INDEX ----------
        public IActionResult Index()
        {
            if (!HasPermission("Manage_Route_Stops"))
                return RedirectToAction("Index", "AccessDenied");

            var list = new List<RouteStopVm>();

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            const string sql = @"
                SELECT RS.Id, RS.RouteId, R.Name AS RouteName,
                       RS.TerminalId, T.Name AS TerminalName,
                       RS.Sequence, RS.DwellMinutes
                FROM dbo.RouteStops RS
                INNER JOIN dbo.BusRoutes R ON RS.RouteId = R.Id
                INNER JOIN dbo.Areas T ON RS.TerminalId = T.Id
                ORDER BY RS.RouteId, RS.Sequence;
            ";

            using (var cmd = new SqlCommand(sql, conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new RouteStopVm
                    {
                        Id = Convert.ToInt32(r["Id"]),
                        BusRouteId = Convert.ToInt32(r["RouteId"]),
                        RouteName = r["RouteName"]?.ToString() ?? "",
                        TerminalId = Convert.ToInt32(r["TerminalId"]),
                        TerminalName = r["TerminalName"]?.ToString() ?? "",
                        Sequence = Convert.ToInt32(r["Sequence"]),
                        DwellMinutes = Convert.ToInt32(r["DwellMinutes"])
                    });
                }
            }

            ViewBag.BusRoutes = GetBusRoutes(conn).ToList();
            ViewBag.Terminals = GetTerminals(conn).ToList();

            return View(list);
        }

        // ---------- SAVE ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save([FromForm] RouteStopVm model)
        {
            if (!HasPermission("Manage_Route_Stops"))
                return RedirectToAction("Index", "AccessDenied");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Validation failed. See details below.";
                TempData["ErrorDetailsHtml"] = BuildErrorDetailsHtml(ModelState);
                TempData["ShowRouteStopModal"] = "true";
                PreserveForm(model);
                return RedirectToAction("Index");
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                if (model.Id == 0)
                {
                    const string insertSql = @"
                        INSERT INTO dbo.RouteStops (RouteId, TerminalId, Sequence, DwellMinutes)
                        VALUES (@RouteId, @TerminalId, @Sequence, @DwellMinutes);
                        SELECT CAST(SCOPE_IDENTITY() AS int);
                    ";
                    using var cmd = new SqlCommand(insertSql, conn);
                    cmd.Parameters.Add("@RouteId", SqlDbType.Int).Value = model.BusRouteId;
                    cmd.Parameters.Add("@TerminalId", SqlDbType.Int).Value = model.TerminalId;
                    cmd.Parameters.Add("@Sequence", SqlDbType.Int).Value = model.Sequence;
                    cmd.Parameters.Add("@DwellMinutes", SqlDbType.Int).Value = model.DwellMinutes;

                    var newIdObj = cmd.ExecuteScalar();
                    var newId = Convert.ToInt32(newIdObj);
                    TempData["SuccessMessage"] = $"RouteStop #{newId} created successfully!";
                }
                else
                {
                    const string updateSql = @"
                        UPDATE dbo.RouteStops
                        SET RouteId = @RouteId,
                            TerminalId = @TerminalId,
                            Sequence = @Sequence,
                            DwellMinutes = @DwellMinutes
                        WHERE Id = @Id;
                    ";
                    using var cmd = new SqlCommand(updateSql, conn);
                    cmd.Parameters.Add("@RouteId", SqlDbType.Int).Value = model.BusRouteId;
                    cmd.Parameters.Add("@TerminalId", SqlDbType.Int).Value = model.TerminalId;
                    cmd.Parameters.Add("@Sequence", SqlDbType.Int).Value = model.Sequence;
                    cmd.Parameters.Add("@DwellMinutes", SqlDbType.Int).Value = model.DwellMinutes;
                    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;

                    var rows = cmd.ExecuteNonQuery();
                    TempData["SuccessMessage"] = rows == 0
                        ? $"No RouteStop found with Id = {model.Id}."
                        : $"RouteStop #{model.Id} updated successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unexpected Error: " + ex.Message;
                TempData["ErrorDetailsHtml"] = $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
                TempData["ShowRouteStopModal"] = "true";
                PreserveForm(model);
            }

            return RedirectToAction("Index");
        }

        // ---------- DELETE ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!HasPermission("Manage_Route_Stops"))
                return RedirectToAction("Index", "AccessDenied");

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                const string sql = "DELETE FROM dbo.RouteStops WHERE Id = @Id;";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                var rows = cmd.ExecuteNonQuery();
                TempData["SuccessMessage"] = rows == 0
                    ? $"No RouteStop found with Id = {id}."
                    : "RouteStop deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unexpected Error: " + ex.Message;
                TempData["ErrorDetailsHtml"] = $"<pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
            }

            return RedirectToAction("Index");
        }

        // ---------- HELPERS ----------
        private void PreserveForm(RouteStopVm model)
        {
            TempData["Form.Id"] = model.Id;
            TempData["Form.BusRouteId"] = model.BusRouteId;
            TempData["Form.TerminalId"] = model.TerminalId;
            TempData["Form.Sequence"] = model.Sequence;
            TempData["Form.DwellMinutes"] = model.DwellMinutes;
        }

        private static IEnumerable<(int Id, string Name)> GetBusRoutes(SqlConnection conn)
        {
            const string sql = "SELECT Id, Name FROM dbo.BusRoutes ORDER BY Name;";
            using var cmd = new SqlCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            var routes = new List<(int Id, string Name)>();
            while (r.Read())
                routes.Add((Convert.ToInt32(r["Id"]), r["Name"]?.ToString() ?? ""));
            return routes;
        }

        private static IEnumerable<(int Id, string Name)> GetTerminals(SqlConnection conn)
        {
            const string sql = "SELECT Id, Name FROM dbo.Areas ORDER BY Name;";
            using var cmd = new SqlCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            var terminals = new List<(int Id, string Name)>();
            while (r.Read())
                terminals.Add((Convert.ToInt32(r["Id"]), r["Name"]?.ToString() ?? ""));
            return terminals;
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

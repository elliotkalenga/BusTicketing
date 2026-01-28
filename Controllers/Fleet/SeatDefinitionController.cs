using BusTicketing.Filters;
using BusTicketing.Helpers;
using BusTicketing.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BusTicketing.Controllers
{
    [Auth]
    public class SeatDefinitionController : Controller
    {
        private readonly IConfiguration _config;
        public SeatDefinitionController(IConfiguration config) => _config = config;

        // ---------------- INDEX ----------------
        public IActionResult Index()
        {
            if (!HasPermission("Manage_Seat_Definitions"))
                return RedirectToAction("Index", "AccessDenied");

            var list = new List<SeatDefinitionFormVm>();

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            const string sql = @"
SELECT SD.Id,
       SD.SeatLayoutId,
       SL.Name,
       SL.Rows,
       SL.Columns,
       SD.SeatNumber,
       SD.IsAisle,
       SD.Class
FROM dbo.SeatDefinitions SD
INNER JOIN dbo.SeatLayouts SL ON SD.SeatLayoutId = SL.Id
ORDER BY SD.Id DESC;";

            using var cmd = new SqlCommand(sql, conn);
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new SeatDefinitionFormVm
                {
                    Id = (int)r["Id"],
                    SeatLayoutId = (int)r["SeatLayoutId"],
                    SeatNumber = r["SeatNumber"].ToString()!,
                    IsAisle = (bool)r["IsAisle"],
                    Class = (int)r["Class"],
                    SeatLayoutDisplay = $"{r["Name"]} ({r["Rows"]} x {r["Columns"]})"
                });
            }

            // ✅ THESE TWO LINES WERE MISSING
            ViewBag.SeatLayouts = GetSeatLayouts();
            ViewBag.SeatClasses = GetSeatClasses();

            return View(list);
        }

        // ---------------- SAVE ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(SeatDefinitionFormVm model)
        {
            if (!HasPermission("Manage_Seat_Definitions"))
                return RedirectToAction("Index", "AccessDenied");

            model.SeatNumber = model.SeatNumber?.Trim().ToUpper();

            if (!ModelState.IsValid)
            {
                PreserveForm(model);
                TempData["ErrorMessage"] = "Validation failed.";
                TempData["ShowSeatModal"] = "true";
                return RedirectToAction("Index");
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                if (model.Id == 0)
                {
                    const string insertSql = @"
INSERT INTO dbo.SeatDefinitions
(SeatLayoutId, SeatNumber, IsAisle, Class)
VALUES
(@SeatLayoutId, @SeatNumber, @IsAisle, @Class);";

                    using var cmd = new SqlCommand(insertSql, conn);
                    AddParams(cmd, model);
                    cmd.ExecuteNonQuery();

                    TempData["SuccessMessage"] = "Seat created successfully!";
                }
                else
                {
                    const string updateSql = @"
UPDATE dbo.SeatDefinitions
SET SeatLayoutId=@SeatLayoutId,
    SeatNumber=@SeatNumber,
    IsAisle=@IsAisle,
    Class=@Class
WHERE Id=@Id;";

                    using var cmd = new SqlCommand(updateSql, conn);
                    AddParams(cmd, model);
                    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;
                    cmd.ExecuteNonQuery();

                    TempData["SuccessMessage"] = "Seat updated successfully!";
                }
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                PreserveForm(model);
                TempData["ErrorMessage"] = "Seat number already exists in this layout.";
                TempData["ShowSeatModal"] = "true";
            }

            return RedirectToAction("Index");
        }

        // ---------------- DELETE (HARD DELETE) ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new SqlCommand("DELETE FROM dbo.SeatDefinitions WHERE Id=@Id", conn);
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "Seat deleted successfully!";
            return RedirectToAction("Index");
        }

        // ---------------- HELPERS ----------------
        private static void AddParams(SqlCommand cmd, SeatDefinitionFormVm m)
        {
            cmd.Parameters.Add("@SeatLayoutId", SqlDbType.Int).Value = m.SeatLayoutId;
            cmd.Parameters.Add("@SeatNumber", SqlDbType.NVarChar, 8).Value = m.SeatNumber!;
            cmd.Parameters.Add("@IsAisle", SqlDbType.Bit).Value = m.IsAisle;
            cmd.Parameters.Add("@Class", SqlDbType.Int).Value = m.Class;
        }

        private void PreserveForm(SeatDefinitionFormVm m)
        {
            TempData["Form.Id"] = m.Id;
            TempData["Form.SeatLayoutId"] = m.SeatLayoutId;
            TempData["Form.SeatNumber"] = m.SeatNumber;
            TempData["Form.IsAisle"] = m.IsAisle;
            TempData["Form.Class"] = m.Class;
        }

        private IEnumerable<dynamic> GetSeatLayouts()
        {
            const string sql = "SELECT Id, Name, Rows, Columns FROM dbo.SeatLayouts";

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            using var r = cmd.ExecuteReader();

            var list = new List<dynamic>();
            while (r.Read())
            {
                list.Add(new
                {
                    Id = (int)r["Id"],
                    Display = $"{r["Name"]} ({r["Rows"]}x{r["Columns"]})"
                });
            }

            return list;
        }

        private static IEnumerable<(int value, string text)> GetSeatClasses()
        {
            yield return (0, "Economy");
            yield return (1, "Business");
            yield return (2, "VIP");
        }

        private bool HasPermission(string p)
        {
            var perms = HttpContext.Session.GetString("Permissions");
            return perms?.Split(',').Contains(p) == true;
        }
    }
}

using BusTicketing.Filters;
using BusTicketing.Helpers;
using BusTicketing.Models.Company;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;


namespace BusTicketing.Controllers.Company
{
    [Auth]
    public class AgencyController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IStringLocalizer<BusTicketing.AppResource> _localizer;

        public AgencyController(IConfiguration config,
            IStringLocalizer<BusTicketing.AppResource> localizer)
        {
            _config = config;
            _localizer = localizer;
        }

        // ---------------- INDEX ----------------
        public IActionResult Index()
        {
            if (!HasPermission("Manage_Agencies"))
                return RedirectToAction("Index", "AccessDenied");

            var agencies = new List<Agency>();

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var sql = @"
                SELECT a.Id, a.CompanyId, a.Name, a.Location, a.Phone,
                       a.RegistrationNumber, a.ServiceProCompany, a.Country,
                       c.Name AS CompanyName
                FROM Agencies a
                INNER JOIN Companies c ON a.CompanyId = c.Id
                ORDER BY a.Id DESC";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                agencies.Add(new Agency
                {
                    Id = (int)reader["Id"],
                    CompanyId = (int)reader["CompanyId"],
                    Name = reader["Name"].ToString(),
                    Location = reader["Location"].ToString(),
                    Phone = reader["Phone"].ToString(),
                    RegistrationNumber = reader["RegistrationNumber"].ToString(),
                    ServiceProCompany = reader["ServiceProCompany"].ToString(),
                    Country = reader["Country"].ToString(),
                    CompanyName = reader["CompanyName"].ToString()
                });
            }

            ViewBag.Companies = LoadCompanies();
            return View(agencies);
        }

        // ---------------- SAVE ----------------
        [HttpPost]
        public IActionResult Save(Agency model)
        {
            if (!HasPermission("Manage_Agencies"))
                return RedirectToAction("Index", "AccessDenied");

            if (model.CompanyId == 0 || string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["ErrorMessage"] = _localizer["AllFieldsRequired"].Value;
                return RedirectToAction("Index");
            }

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            if (model.Id == 0)
            {
                var sql = @"
                    INSERT INTO Agencies
                    (CompanyId, Name, Location, Phone, RegistrationNumber, ServiceProCompany, Country)
                    VALUES
                    (@CompanyId, @Name, @Location, @Phone, @RegistrationNumber, @ServiceProCompany, @Country)";

                using var cmd = new SqlCommand(sql, conn);
                FillParams(cmd, model);
                cmd.ExecuteNonQuery();

                TempData["SuccessMessage"] = _localizer["AgencyCreated"].Value;
            }
            else
            {
                var sql = @"
                    UPDATE Agencies SET
                        CompanyId=@CompanyId,
                        Name=@Name,
                        Location=@Location,
                        Phone=@Phone,
                        RegistrationNumber=@RegistrationNumber,
                        ServiceProCompany=@ServiceProCompany,
                        Country=@Country
                    WHERE Id=@Id";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", model.Id);
                FillParams(cmd, model);
                cmd.ExecuteNonQuery();

                TempData["SuccessMessage"] = _localizer["AgencyUpdated"].Value;
            }

            return RedirectToAction("Index");
        }

        // ---------------- DELETE ----------------
        [HttpPost]
        public IActionResult Delete(int id)
        {
            if (!HasPermission("Manage_Agencies"))
                return RedirectToAction("Index", "AccessDenied");

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var sql = "DELETE FROM Agencies WHERE Id=@Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = _localizer["AgencyDeleted"].Value;
            return RedirectToAction("Index");
        }

        // ---------------- HELPERS ----------------
        private void FillParams(SqlCommand cmd, Agency a)
        {
            cmd.Parameters.AddWithValue("@CompanyId", a.CompanyId);
            cmd.Parameters.AddWithValue("@Name", a.Name);
            cmd.Parameters.AddWithValue("@Location", a.Location ?? "");
            cmd.Parameters.AddWithValue("@Phone", a.Phone ?? "");
            cmd.Parameters.AddWithValue("@RegistrationNumber", a.RegistrationNumber ?? "");
            cmd.Parameters.AddWithValue("@ServiceProCompany", a.ServiceProCompany ?? "");
            cmd.Parameters.AddWithValue("@Country", a.Country ?? "");
        }

        private bool HasPermission(string permission)
        {
            var perms = HttpContext.Session.GetString("Permissions");
            return !string.IsNullOrEmpty(perms) && perms.Split(',').Contains(permission);
        }

        private List<Models.Company.Company> LoadCompanies()
        {
            var list = new List<Models.Company.Company>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var sql = "SELECT Id, Name FROM Companies ORDER BY Name";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new Models.Company.Company
                {
                    Id = (int)reader["Id"],
                    Name = reader["Name"].ToString()
                });
            }
            return list;
        }
    }
}

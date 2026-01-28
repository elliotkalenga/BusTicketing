using BusTicketing.Filters;
using BusTicketing.Helpers;
using BusTicketing.Models.Auth;
using BusTicketing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace BusTicketing.Controllers
{
    [Auth]
    public class UsersController : Controller
    {
        private readonly IConfiguration _config;
        private readonly SmsService _smsService;

        public UsersController(IConfiguration config, SmsService smsService)
        {
            _config = config;
            _smsService = smsService;
        }

        // ------------------- INDEX ------------------------
        public IActionResult Index()
        {
            int? agencyId = HttpContext.Session.GetInt32("AgencyId");

            if (!HasPermission("View_Users"))
                return RedirectToAction("Index", "AccessDenied");
            List<User> users = new List<User>();

            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                string sql = @"SELECT u.Id, u.Username, u.FullName, 
       u.Email, u.Phone, u.IsActive,
       u.AreaId,
       ag.Name AS AgencyName,
       a.Name AS AreaName,T.name as TownName, P.Name as ProvinceName
FROM Users u
INNER JOIN Areas a ON u.AreaId = a.Id
INNER JOIN Agencies ag ON u.AgencyId = ag.Id
inner Join Towns T on a.Townid=t.id
Inner Join Provinces p on t.ProvinceId=p.id
WHERE u.AgencyId=@AgencyId
";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AgencyId", agencyId ?? (object)DBNull.Value);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new User
                    {
                        Id = (int)reader["Id"],
                        Username = reader["Username"].ToString(),
                        FullName = reader["FullName"].ToString(),
                        Email = reader["Email"].ToString(),
                        Phone = reader["Phone"].ToString(),
                        AgencyName = reader["AgencyName"].ToString(),
                        AreaName = reader["AreaName"].ToString(),
                        TownName = reader["TownName"].ToString(),
                        ProvinceName = reader["ProvinceName"].ToString(),
                        AreaId = reader["AreaId"] == DBNull.Value ? null : (int?)reader["AreaId"],
                        IsActive = (bool)reader["IsActive"]
                    });
                }
            }
            ViewBag.Areas = GetAreas();

            return View(users);
        }

        // ------------------- ADD / EDIT ------------------------
        [HttpPost]
        public async Task<IActionResult> Save(User model)
        {
            if (!HasPermission("Create_Users"))
                return RedirectToAction("Index", "AccessDenied");
            try
            {
                int? agencyId = HttpContext.Session.GetInt32("AgencyId");

                string plainPassword = null;
                string hashedPassword = null;

                // Generate a password only if new user
                if (model.Id == 0)
                {
                    hashedPassword = GenerateRandomPasswordHash(out plainPassword);
                }
                else
                {
                    hashedPassword = string.IsNullOrEmpty(model.PasswordHash)
                        ? null
                        : HashPassword(model.PasswordHash);
                }

                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();
                    SqlCommand cmd;

                    if (model.Id == 0)
                    {
                        string sql = @"INSERT INTO Users
                            (Username, PasswordHash, FullName, Email, Phone, IsActive, AgencyId, CreatedAt,AreaId)
                            VALUES
                            (@Username, @PasswordHash, @FullName, @Email, @Phone, @IsActive, @AgencyId, @CreatedAt,@AreaId)";

                        cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                    }
                    else
                    {
                        string sql = @"UPDATE Users SET
                            Username=@Username, FullName=@FullName, Email=@Email, Phone=@Phone,AreaId=@AreaId,
                            IsActive=@IsActive, AgencyId=@AgencyId {0}
                            WHERE Id=@Id";

                        string passClause = hashedPassword != null ? ", PasswordHash=@PasswordHash" : "";
                        sql = string.Format(sql, passClause);

                        cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@Id", model.Id);

                        if (hashedPassword != null)
                            cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                    }

                    cmd.Parameters.AddWithValue("@Username", model.Username);
                    cmd.Parameters.AddWithValue("@FullName", model.FullName ?? "");
                    cmd.Parameters.AddWithValue("@Email", model.Email ?? "");
                    cmd.Parameters.AddWithValue("@Phone", model.Phone ?? "");
                    cmd.Parameters.AddWithValue("@AreaId", model.AreaId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", model.IsActive);
                    cmd.Parameters.AddWithValue("@AgencyId", agencyId ?? (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                }

                // ------------------- SEND SMS WHEN NEW USER IS CREATED -------------------
                if (model.Id == 0 && !string.IsNullOrWhiteSpace(model.Phone))
                {
                    string accessToken = await _smsService.GetAccessTokenAsync();

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        // Use BaseUrl from appsettings.json
                        string baseUrl = _config["AppSettings:BaseUrl"];
                        string loginUrl = $"{baseUrl}/Login";

                        string smsBody =
$@"Hello {model.FullName},
Your user account has been created in 
Fecomas Bus Ticketing System.
using this link to reset your password {loginUrl}";

                        await _smsService.SendSmsAsync(accessToken,
                            new[] { model.Phone },
                            smsBody
                        );
                    }
                }

                TempData["SuccessMessage"] = model.Id == 0
                    ? "User created successfully! SMS sent."
                    : "User updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ------------------- DELETE ------------------------
        [HttpPost]
        public IActionResult Delete(int id)
        {
            if (!HasPermission("Delete_Users"))
                return RedirectToAction("Index", "AccessDenied");
            try
            {
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();
                    string sql = "DELETE FROM Users WHERE Id=@Id";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = "User deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Index");
        }
        private List<SelectListItem> GetAreas()
        {
            var list = new List<SelectListItem>();

            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                string sql = @"SELECT Id, Name FROM Areas ORDER BY Name";
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new SelectListItem
                    {
                        Value = reader["Id"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }
            }

            return list;
        }

        // ------------------- HELPERS ------------------------
        private bool HasPermission(string permission)
        {
            var perms = HttpContext.Session.GetString("Permissions");
            return !string.IsNullOrEmpty(perms) && perms.Split(',').Contains(permission);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        private string GenerateRandomPasswordHash(out string plainPassword)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$!";
            var random = new Random();
            plainPassword = new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return HashPassword(plainPassword);
        }
    }
}

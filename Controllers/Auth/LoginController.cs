using BusTicketing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace BusTicketing.Controllers.Auth
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _config;
        private readonly SmsService _smsService;

        public LoginController(IConfiguration config, SmsService smsService)
        {
            _config = config;
            _smsService = smsService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Enter username and password";
                return View();
            }

            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();

                // Fetch user info + branch + company
                string sql = @"
            SELECT TOP 1 
    u.Id, u.Username, u.PasswordHash, ur.RoleId, 
    u.FullName, u.Email, u.Phone, u.AgencyId,
    b.Name AS AgencyName, b.CompanyId AS CompanyId, c.Name AS CompanyName
FROM Users u
INNER JOIN UserRoles ur ON u.Id = ur.UserId
LEFT JOIN Agencies b ON u.AgencyId = b.Id
LEFT JOIN Companies c ON b.CompanyId = c.Id
WHERE u.Username = @Username";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Username", username);

                SqlDataReader reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    ViewBag.Error = "Invalid username or password";
                    return View();
                }

                int userId = Convert.ToInt32(reader["Id"]);
                string usernameDb = reader["Username"].ToString();
                string passwordHashDb = reader["PasswordHash"].ToString();
                int roleId = Convert.ToInt32(reader["RoleId"]);

                string fullName = reader["FullName"].ToString();
                string email = reader["Email"].ToString();
                string phone = reader["Phone"].ToString();
                int agencyId = reader["AgencyId"] != DBNull.Value ? Convert.ToInt32(reader["AgencyId"]) : 0;
                string AgencyName = reader["AgencyName"].ToString();
                int companyId = reader["CompanyId"] != DBNull.Value ? Convert.ToInt32(reader["CompanyId"]) : 0;
                string companyName = reader["CompanyName"].ToString();

                reader.Close();

                //------------------- HASH VALIDATION ------------------------
                if (!VerifyHashedPassword(password, passwordHashDb))
                {
                    ViewBag.Error = "Invalid username or password";
                    return View();
                }

                // Load permission names
                string permSql = @"
            SELECT p.Code
            FROM Permissions p
            INNER JOIN RolePermissions rp ON p.Id = rp.PermissionId
            WHERE rp.RoleId = @RoleId";

                SqlCommand permCmd = new SqlCommand(permSql, conn);
                permCmd.Parameters.AddWithValue("@RoleId", roleId);

                SqlDataReader permReader = permCmd.ExecuteReader();
                List<string> permissions = new List<string>();
                while (permReader.Read())
                {
                    permissions.Add(permReader["Code"].ToString());
                }
                permReader.Close();

                // ------------------- CREATE SESSION ------------------------
                HttpContext.Session.SetInt32("UserId", userId);
                HttpContext.Session.SetString("Username", usernameDb);
                HttpContext.Session.SetInt32("RoleId", roleId);
                HttpContext.Session.SetString("FullName", fullName);
                HttpContext.Session.SetString("Email", email);
                HttpContext.Session.SetString("Phone", phone);
                HttpContext.Session.SetInt32("AgencyId", agencyId);
                HttpContext.Session.SetString("AgencyName", AgencyName);
                HttpContext.Session.SetInt32("CompanyId", companyId);
                HttpContext.Session.SetString("CompanyName", companyName);
                HttpContext.Session.SetString("Permissions", string.Join(",", permissions));

                return RedirectToAction("Index", "Dashboard");
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        // ------------------- OTP RESET PASSWORD ------------------------

        [HttpPost]
        public async Task<IActionResult> SendOtp([FromBody] PhoneRequest request)
        {
            if (string.IsNullOrEmpty(request.Phone))
                return Json(new { success = false, message = "Phone number required." });

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var sql = "SELECT TOP 1 Id, FullName, Username FROM Users WHERE Phone=@Phone";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Phone", request.Phone);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return Json(new { success = false, message = "No account associated with this phone." });

            var userId = Convert.ToInt32(reader["Id"]);
            var fullName = reader["FullName"].ToString();
            reader.Close();

            var otp = new Random().Next(100000, 999999).ToString();

            // Store all required OTP info in session
            HttpContext.Session.SetString("PasswordResetUserId", userId.ToString());
            HttpContext.Session.SetString("PasswordResetOTP", otp);
            HttpContext.Session.SetString("PasswordResetPhone", request.Phone);

            var token = await _smsService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
                await _smsService.SendSmsAsync(token, new string[] { request.Phone }, $"Hello {fullName}, your OTP is {otp}");

            return Json(new { success = true, message = "OTP sent to your phone." });
        }

        [HttpPost]
        public IActionResult VerifyOtp([FromBody] OtpRequest request)
        {
            string sessionOtp = HttpContext.Session.GetString("PasswordResetOTP");
            return sessionOtp == request.Otp
                ? Json(new { success = true, message = "OTP verified." })
                : Json(new { success = false, message = "Invalid OTP." });
        }


        [HttpPost]
        public async Task<IActionResult> SaveNewPassword([FromBody] NewPasswordRequest request)
        {
            if (request.Pass != request.Conf)
                return Json(new { success = false, message = "Passwords do not match." });

            string userIdStr = HttpContext.Session.GetString("PasswordResetUserId");
            if (!int.TryParse(userIdStr, out int userId))
                return Json(new { success = false, message = "Session expired. Try again." });

            string hashedPassword = HashPassword(request.Pass);

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var sql = "UPDATE Users SET PasswordHash=@PasswordHash WHERE Id=@Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
            cmd.Parameters.AddWithValue("@Id", userId);
            cmd.ExecuteNonQuery();

            // Optionally, send SMS with username
            string phone = HttpContext.Session.GetString("PasswordResetPhone");
            var token = await _smsService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                string username = GetUsername(userId);
                await _smsService.SendSmsAsync(token, new string[] { phone }, $"Your username is {username}. Use the password you just reset to login.");
            }

            // Clear OTP session after successful reset
            HttpContext.Session.Remove("PasswordResetUserId");
            HttpContext.Session.Remove("PasswordResetOTP");
            HttpContext.Session.Remove("PasswordResetPhone");

            return Json(new { success = true, message = "Password reset successfully. SMS sent with your username." });
        }

        // Clear OTP when modal closes
        [HttpPost]
        public IActionResult ClearOtp()
        {
            HttpContext.Session.Remove("PasswordResetUserId");
            HttpContext.Session.Remove("PasswordResetOTP");
            HttpContext.Session.Remove("PasswordResetPhone");
            return Ok();
        }

        private string GetUsername(int userId)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var sql = "SELECT Username FROM Users WHERE Id=@Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", userId);
            return cmd.ExecuteScalar()?.ToString();
        }

        private bool VerifyHashedPassword(string password, string storedHash)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            foreach (var b in bytes)
                builder.Append(b.ToString("x2"));
            return builder.ToString() == storedHash;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            foreach (var b in bytes)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }

        // ------------------- REQUEST MODELS ------------------------
        public class PhoneRequest { public string Phone { get; set; } }
        public class OtpRequest { public string Otp { get; set; } }
        public class NewPasswordRequest { public string Pass { get; set; } public string Conf { get; set; } }
    }
}

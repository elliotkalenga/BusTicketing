using BusTicketing.Filters;
using BusTicketing.Helpers;
using BusTicketing.Models.Auth;
using BusTicketing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace BusTicketing.Controllers
{
    [Auth]
    public class ChangePasswordController : Controller
    {
        private readonly IConfiguration _config;
        private readonly SmsService _smsService;

        public ChangePasswordController(IConfiguration config, SmsService smsService)
        {
            _config = config;
            _smsService = smsService;
        }

        // ------------------- INDEX (Admin View) ------------------------
        public IActionResult Index()
        {
            List<User> users = new List<User>();

            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                string sql = "SELECT Id, Username, FullName, Email, Phone FROM Users";
                SqlCommand cmd = new SqlCommand(sql, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    users.Add(new User
                    {
                        Id = (int)reader["Id"],
                        Username = reader["Username"].ToString(),
                        FullName = reader["FullName"].ToString(),
                        Email = reader["Email"].ToString(),
                        Phone = reader["Phone"].ToString()
                    });
                }
            }

            return View(users);
        }

        // ------------------- RESET PASSWORD (Admin) ------------------------
        [Permission("Reset_Password")]

        [HttpPost]
        public async Task<IActionResult> ResetPassword(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid user ID!";
                return RedirectToAction("Index");
            }

            try
            {
                // Generate new password
                string plainPassword = GenerateRandomPassword(out string hashedPassword);

                string username = "";
                string phone = "";
                string fullName = "";

                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();

                    // Get user details
                    string selectSql = "SELECT Username, Phone, FullName FROM Users WHERE Id=@Id";
                    SqlCommand selectCmd = new SqlCommand(selectSql, conn);
                    selectCmd.Parameters.AddWithValue("@Id", id);
                    var reader = selectCmd.ExecuteReader();
                    if (reader.Read())
                    {
                        username = reader["Username"].ToString();
                        phone = reader["Phone"].ToString();
                        fullName = reader["FullName"].ToString();
                    }
                    reader.Close();

                    // Update password in DB
                    string updateSql = "UPDATE Users SET PasswordHash=@PasswordHash WHERE Id=@Id";
                    SqlCommand updateCmd = new SqlCommand(updateSql, conn);
                    updateCmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                    updateCmd.Parameters.AddWithValue("@Id", id);
                    updateCmd.ExecuteNonQuery();
                }

                // ------------------- SEND SMS -------------------
                bool smsSent = false;
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    try
                    {
                        string accessToken = await _smsService.GetAccessTokenAsync();
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            string baseUrl = _config["AppSettings:BaseUrl"];
                            string loginUrl = $"{baseUrl}/Login";

                            string smsBody =
$@"Password Reset for {fullName},

Username: {username}
Password: {plainPassword}

Login here: {loginUrl}";

                            await _smsService.SendSmsAsync(accessToken, new[] { phone }, smsBody);
                            smsSent = true;
                        }
                    }
                    catch
                    {
                        smsSent = false;
                    }
                }

                // ------------------- SHOW SUCCESS WITH PASSWORD -------------------
                TempData["SuccessMessage"] = smsSent
                    ? $"Password reset successfully! New Password: {plainPassword}. SMS sent."
                    : $"Password reset successfully! New Password: {plainPassword}. SMS attempted.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ------------------- CHANGE OWN PASSWORD ------------------------
        [HttpGet]
        public IActionResult ChangeOwnPassword()
        {
            // Ensure user is logged in
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToAction("Index", "Login");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangeOwnPassword(string currentPassword, string newPassword, string confirmPassword)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "You must be logged in to change password.";
                return RedirectToAction("Index", "Login");
            }

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["ErrorMessage"] = "All fields are required.";
                return RedirectToAction("ChangeOwnPassword");
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation do not match.";
                return RedirectToAction("ChangeOwnPassword");
            }

            string phone = "";
            string username = "";
            string fullName = "";

            try
            {
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();

                    // Get current user details and password hash
                    string selectSql = "SELECT PasswordHash, Phone, Username, FullName FROM Users WHERE Id=@Id";
                    SqlCommand selectCmd = new SqlCommand(selectSql, conn);
                    selectCmd.Parameters.AddWithValue("@Id", userId.Value);
                    var reader = selectCmd.ExecuteReader();
                    string currentHash = null;
                    if (reader.Read())
                    {
                        currentHash = reader["PasswordHash"].ToString();
                        phone = reader["Phone"].ToString();
                        username = reader["Username"].ToString();
                        fullName = reader["FullName"].ToString();
                    }
                    reader.Close();

                    if (currentHash != HashPassword(currentPassword))
                    {
                        TempData["ErrorMessage"] = "Current password is incorrect.";
                        return RedirectToAction("ChangeOwnPassword");
                    }

                    // Update password
                    string newHash = HashPassword(newPassword);
                    string updateSql = "UPDATE Users SET PasswordHash=@PasswordHash WHERE Id=@Id";
                    SqlCommand updateCmd = new SqlCommand(updateSql, conn);
                    updateCmd.Parameters.AddWithValue("@PasswordHash", newHash);
                    updateCmd.Parameters.AddWithValue("@Id", userId.Value);
                    updateCmd.ExecuteNonQuery();
                }

                // ------------------- SEND SMS -------------------
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    try
                    {
                        string accessToken = await _smsService.GetAccessTokenAsync();
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            string baseUrl = _config["AppSettings:BaseUrl"];
                            string loginUrl = $"{baseUrl}/Login";

                            string smsBody =
        $@"Password Changed For {fullName},
Username: {username}
Password: {newPassword}
Login here: {loginUrl}";
                            await _smsService.SendSmsAsync(accessToken, new[] { phone }, smsBody);
                        }
                    }
                    catch
                    {
                        // Ignore SMS errors, optionally log them
                    }
                }

                TempData["SuccessMessage"] = "Password changed successfully! SMS attempted.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            return RedirectToAction("ChangeOwnPassword");
        }

        // ------------------- HELPERS ------------------------
        private string GenerateRandomPassword(out string hashedPassword)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$!";
            var random = new Random();
            string plainPassword = new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            hashedPassword = HashPassword(plainPassword);
            return plainPassword;
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
    }
}

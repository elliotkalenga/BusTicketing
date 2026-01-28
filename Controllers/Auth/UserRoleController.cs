using BusTicketing.Filters;
using BusTicketing.Helpers;
using BusTicketing.Models.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BusTicketing.Controllers.Au
{
[Auth]
    public class UserRoleController : Controller
    {
        private readonly IConfiguration _config;

        public UserRoleController(IConfiguration config)
        {
            _config = config;
        }

        // ------------------- INDEX ------------------------
        public IActionResult Index()
        {
            int? agencyId = HttpContext.Session.GetInt32("AgencyId");

            if (!HasPermission("View_Users"))
                return RedirectToAction("Index", "AccessDenied");
            List<UserRole> userRoles = new List<UserRole>();

            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                string sql = @"
                    SELECT ur.Id, ur.UserId, ur.RoleId, u.Username, r.Name AS RoleName, ur.AssignedAt
                    FROM UserRoles ur
                    INNER JOIN Users u ON ur.UserId = u.Id
                    INNER JOIN Roles r ON ur.RoleId = r.Id
                    Where Ur.AgencyId=@AgencyId
                    ORDER BY ur.Id DESC";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AgencyId", agencyId ?? (object)DBNull.Value);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    userRoles.Add(new UserRole
                    {
                        Id = (int)reader["Id"],
                        UserId = (int)reader["UserId"],
                        RoleId = (int)reader["RoleId"],
                        AssignedAt = (DateTime)reader["AssignedAt"],
                        User = new User { Username = reader["Username"].ToString() },
                        Role = new Role { Name = reader["RoleName"].ToString() }
                    });
                }
            }

            ViewBag.Users = LoadUsers();
            ViewBag.Roles = LoadRoles();

            return View(userRoles);
        }

        // ------------------- SAVE MULTIPLE ROLES ------------------------
        [HttpPost]
        public IActionResult Save(int UserId, int[] RoleIds)
        {
            if (!HasPermission("Assign_Role_to_Users"))
                return RedirectToAction("Index", "AccessDenied");
            try
            {
                int? agencyId = HttpContext.Session.GetInt32("AgencyId");

                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();

                    // Delete old roles
                    string deleteSql = "DELETE FROM UserRoles WHERE UserId=@UserId AND AgencyId=@AgencyId";
                    SqlCommand delCmd = new SqlCommand(deleteSql, conn);
                    delCmd.Parameters.AddWithValue("@UserId", UserId);
                    delCmd.Parameters.AddWithValue("@AgencyId", agencyId ?? (object)DBNull.Value);
                    delCmd.ExecuteNonQuery();

                    // Insert new roles (branch-controlled)
                    foreach (var roleId in RoleIds)
                    {
                        string insertSql = @"
                            INSERT INTO UserRoles (UserId, RoleId, AssignedAt, AgencyId)
                            VALUES (@UserId, @RoleId, @AssignedAt, @AgencyId)";

                        SqlCommand cmd = new SqlCommand(insertSql, conn);
                        cmd.Parameters.AddWithValue("@UserId", UserId);
                        cmd.Parameters.AddWithValue("@RoleId", roleId);
                        cmd.Parameters.AddWithValue("@AssignedAt", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("@AgencyId", agencyId ?? (object)DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "Roles updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ------------------- DELETE ------------------------
        [HttpPost]
        public IActionResult Delete(int id)
        {
            if (!HasPermission("Remove_Role_from_Users"))
                return RedirectToAction("Index", "AccessDenied");
            try
            {
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();
                    string sql = "DELETE FROM UserRoles WHERE Id=@Id";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = "Role assignment removed!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ------------------- HELPERS ------------------------
        private List<User> LoadUsers()
        {
            int? agencyId = HttpContext.Session.GetInt32("AgencyId");

            var list = new List<User>();
            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                string sql = "SELECT u.Id, u.Username,a.Name as AgencyName,u.AgencyId " +
                    "FROM Users u inner join Agencies a on u.agencyid=a.id" +
                    " WHERE AgencyId=@AgencyId ORDER BY Username";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AgencyId", agencyId ?? (object)DBNull.Value);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new User
                    {
                        Id = (int)reader["Id"],
                        Username = reader["Username"].ToString(),
                        AgencyName = reader["AgencyName"].ToString()
                    });
                }
            }
            return list;
        }

        private List<Role> LoadRoles()
        {
            int? agencyId = HttpContext.Session.GetInt32("AgencyId");

            var list = new List<Role>();
            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                string sql = "SELECT Id, Name FROM Roles WHERE AgencyId=@AgencyId ORDER BY Name";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AgencyId", agencyId ?? (object)DBNull.Value);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Role
                    {
                        Id = (int)reader["Id"],
                        Name = reader["Name"].ToString(),
                    });
                }
            }
            return list;
        }

        private bool HasPermission(string permission)
        {
            var perms = HttpContext.Session.GetString("Permissions");
            if (string.IsNullOrEmpty(perms)) return false;

            return perms.Split(',').Contains(permission);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}

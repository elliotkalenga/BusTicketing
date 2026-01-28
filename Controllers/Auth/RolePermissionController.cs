using BusTicketing.Filters;
using BusTicketing.Helpers;
using BusTicketing.Models.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
namespace BusTicketing.Controllers
{
    [Auth]
    public class RolePermissionController : Controller
    {
        private readonly IConfiguration _config;

        public RolePermissionController(IConfiguration config)
        {
            _config = config;
        }

        // ------------------- INDEX ------------------------
        public IActionResult Index()
        {
            if (!HasPermission("ManagePermissions"))
                return RedirectToAction("Index", "AccessDenied");
            int? agencyId = HttpContext.Session.GetInt32("AgencyId");

            var roles = new List<Role>();

            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();

                // Load only roles that belong to logged-in user branch
                string sqlRoles = "SELECT Id, Name FROM Roles WHERE AgencyId=@AgencyId ORDER BY Name";
                SqlCommand cmdRoles = new SqlCommand(sqlRoles, conn);
                cmdRoles.Parameters.AddWithValue("@AgencyId", agencyId ?? (object)DBNull.Value);

                var readerRoles = cmdRoles.ExecuteReader();
                while (readerRoles.Read())
                {
                    roles.Add(new Role
                    {
                        Id = (int)readerRoles["Id"],
                        Name = readerRoles["Name"].ToString(),
                        RolePermissions = new List<RolePermission>()
                    });
                }
                readerRoles.Close();

                // Load only permissions assigned to roles WITHIN the same branch
                string sqlPerms = @"
                    SELECT rp.Id, rp.RoleId, rp.PermissionId, p.Code AS PermissionCode
                    FROM RolePermissions rp
                    INNER JOIN Permissions p ON rp.PermissionId = p.Id
                    INNER JOIN Roles r ON rp.RoleId = r.Id
                    WHERE rp.AgencyId=@AgencyId AND r.AgencyId=@AgencyId";

                SqlCommand cmdPerms = new SqlCommand(sqlPerms, conn);
                cmdPerms.Parameters.AddWithValue("@AgencyId", agencyId ?? (object)DBNull.Value);

                var readerPerms = cmdPerms.ExecuteReader();
                while (readerPerms.Read())
                {
                    int roleId = (int)readerPerms["RoleId"];
                    var role = roles.FirstOrDefault(r => r.Id == roleId);
                    if (role != null)
                    {
                        role.RolePermissions.Add(new RolePermission
                        {
                            Id = (int)readerPerms["Id"],
                            RoleId = roleId,
                            PermissionId = (int)readerPerms["PermissionId"],
                            Permission = new Permission
                            {
                                Id = (int)readerPerms["PermissionId"],
                                Code = readerPerms["PermissionCode"].ToString()
                            }
                        });
                    }
                }
                readerPerms.Close();
            }

            ViewBag.Permissions = LoadPermissions();
            return View(roles);
        }

        // ------------------- SAVE MULTIPLE PERMISSIONS ------------------------
        [HttpPost]
        public IActionResult Save(int RoleId, int[] PermissionIds)
        {
            if (!HasPermission("ManagePermissions"))
                return RedirectToAction("Index", "AccessDenied");
            if (RoleId <= 0)
            {
                TempData["ErrorMessage"] = "Please select a valid role.";
                return RedirectToAction("Index");
            }

            int? agencyId = HttpContext.Session.GetInt32("AgencyId");
            string? username = HttpContext.Session.GetString("UserName");

            try
            {
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();

                    // Delete old permissions for this role only within the branch
                    string deleteSql = "DELETE FROM RolePermissions WHERE RoleId=@RoleId AND AgencyId=@AgencyId";
                    SqlCommand delCmd = new SqlCommand(deleteSql, conn);
                    delCmd.Parameters.AddWithValue("@RoleId", RoleId);
                    delCmd.Parameters.AddWithValue("@AgencyId", agencyId ?? (object)DBNull.Value);
                    delCmd.ExecuteNonQuery();

                    // Insert new permissions safely
                    foreach (var pid in PermissionIds)
                    {
                        string insertSql = @"
                            INSERT INTO RolePermissions (RoleId, PermissionId, AssignedAt, AgencyId,CreatedBy)
                            VALUES (@RoleId, @PermissionId, @AssignedAt, @AgencyId,@CreatedBy)";

                        SqlCommand cmd = new SqlCommand(insertSql, conn);
                        cmd.Parameters.AddWithValue("@RoleId", RoleId);
                        cmd.Parameters.AddWithValue("@PermissionId", pid);
                        cmd.Parameters.AddWithValue("@AssignedAt", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("@AgencyId", agencyId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedBy", username ?? (object)DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "Permissions updated successfully!";
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
            if (!HasPermission("ManagePermissions"))
                return RedirectToAction("Index", "AccessDenied");
            try
            {
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();

                    string sql = "DELETE FROM RolePermissions WHERE Id=@Id";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = "Permission removed!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ------------------- HELPERS ------------------------
        private List<Permission> LoadPermissions()
        {
            var list = new List<Permission>();
            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                string sql = "SELECT Id, Code, Description FROM Permissions ORDER BY Description, id,code";
                SqlCommand cmd = new SqlCommand(sql, conn);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Permission
                    {
                        Id = (int)reader["Id"],
                        Code = reader["Code"].ToString(),
                        Description = reader["Description"].ToString()
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
    }
}

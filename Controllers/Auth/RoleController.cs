using BusTicketing.Filters;
using BusTicketing.Helpers;
using BusTicketing.Models.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BusTicketing.Controllers
{
[Auth]    
    public class RoleController : Controller
    {
        private readonly IConfiguration _config;

        public RoleController(IConfiguration config)
        {
            _config = config;
        }

        // ------------------- INDEX ------------------------
        public IActionResult Index()
        {
            if (!HasPermission("View_Roles"))
                return RedirectToAction("Index", "AccessDenied");
            List<Role> roles = new List<Role>();

            // Get BranchId from session
            int? agencyId = HttpContext.Session.GetInt32("AgencyId");

            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                string sql = @"SELECT r.Id as RoleId, r.Name as RoleName, a.Name as AgencyName, Description 
FROM Roles r inner join agencies a on r.AgencyId=a.id WHERE r.AgencyId=@AgencyId ORDER BY r.Id DESC";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AgencyId", agencyId ?? (object)DBNull.Value);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    roles.Add(new Role
                    {
                        Id = (int)reader["RoleId"],
                        Name = reader["RoleName"].ToString(),
                        AgencyName= reader["AgencyName"].ToString(),
                        Description = reader["Description"].ToString()
                    });
                }
            }

            return View(roles);
        }

        // ------------------- ADD / EDIT ------------------------
        [HttpPost]
        public IActionResult Save(Role model)
        {
            if (!HasPermission("Create_Roles"))
                return RedirectToAction("Index", "AccessDenied");
            try
            {
                // Get BranchId from session
                int? agencyId = HttpContext.Session.GetInt32("AgencyId");

                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();
                    SqlCommand cmd;

                    if (model.Id == 0)
                    {
                        string sql = @"INSERT INTO Roles (Name, Description, AgencyId) 
                                       VALUES (@Name, @Description, @AgencyId)";
                        cmd = new SqlCommand(sql, conn);
                    }
                    else
                    {
                        string sql = @"UPDATE Roles 
                                       SET Name=@Name, Description=@Description, AgencyId=@AgencyId
                                       WHERE Id=@Id";
                        cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@Id", model.Id);
                    }

                    cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
                    cmd.Parameters.AddWithValue("@Description", model.Description ?? "");
                    cmd.Parameters.AddWithValue("@AgencyId", agencyId ?? (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = model.Id == 0
                    ? "Role created successfully!"
                    : "Role updated successfully!";
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
            if (!HasPermission("Delete_Roles"))
                return RedirectToAction("Index", "AccessDenied");
            try
            {
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();
                    string sql = "DELETE FROM Roles WHERE Id=@Id";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = "Role deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ------------------- PERMISSION CHECK ------------------------
        private bool HasPermission(string permission)
        {
            var perms = HttpContext.Session.GetString("Permissions");
            if (string.IsNullOrEmpty(perms)) return false;

            var list = perms.Split(',');
            return list.Contains(permission);
        }
    }
}

using Microsoft.AspNetCore.Http;
using System.Linq;

namespace BusTicketing.Services
{
    public class PermissionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PermissionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Check if user has a specific permission by name
        public bool HasPermission(string permissionName)
        {
            // ✅ Use the same key as in LoginController
            var permissions = _httpContextAccessor.HttpContext.Session.GetString("Permissions");

            if (string.IsNullOrEmpty(permissions))
                return false;

            var list = permissions.Split(',', System.StringSplitOptions.RemoveEmptyEntries).ToList();

            return list.Contains(permissionName, System.StringComparer.OrdinalIgnoreCase);
        }
    }
}

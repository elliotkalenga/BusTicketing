namespace BusTicketing.Helpers
{
    public static class PermissionHelper
    {
        public static bool HasPermission(HttpContext context, int permissionId)
        {
            var permString = context.Session.GetString("Permissions");
            if (string.IsNullOrEmpty(permString)) return false;

            var list = permString.Split(',').Select(int.Parse).ToList();
            return list.Contains(permissionId);
        }
    }
}

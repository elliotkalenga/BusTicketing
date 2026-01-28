using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace BusTicketing.Filters
{
    public class PermissionAttribute : ActionFilterAttribute
    {
        private readonly string[] _requiredPermissions;

        public PermissionAttribute(params string[] requiredPermissions)
        {
            _requiredPermissions = requiredPermissions;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var permissionsString = httpContext.Session.GetString("Permissions");

            if (string.IsNullOrEmpty(permissionsString))
            {
                // No permissions, redirect to access denied
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                return;
            }

            var userPermissions = permissionsString.Split(',');

            if (!_requiredPermissions.Any(p => userPermissions.Contains(p)))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
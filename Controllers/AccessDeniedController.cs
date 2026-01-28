using Microsoft.AspNetCore.Mvc;

namespace BusTicketing.Controllers
{
    public class AccessDeniedController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

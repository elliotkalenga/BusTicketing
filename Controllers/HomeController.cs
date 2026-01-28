// Controllers/HomeController.cs
using BusTicketing.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BusTicketing.Controllers
{
    public class HomeController : Controller
    {
        private readonly IStringLocalizer<BusTicketing.AppResource> _localizer;

        public HomeController(IStringLocalizer<BusTicketing.AppResource> localizer)
        {
            _localizer = localizer;
        }

        public IActionResult Index()
        {
            // Localized strings from AppResource.*.resx
            ViewData["Title"] = _localizer["Home_Title"];     // e.g., "Welcome to Bus Ticketing!"
            ViewData["Message"] = _localizer["Home_Message"]; // e.g., "This is the English version."
            ViewData["CTA"] = _localizer["Home_CTA"];         // optional call-to-action text

            return View();
        }
    }
}

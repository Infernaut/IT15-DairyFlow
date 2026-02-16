using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IT15_DairyFlow.Controllers
{
    [Authorize(Roles = "Superadmin")]
    public class SuperAdminController : Controller
    {
        [HttpGet]
        public IActionResult Companies()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Subscriptions()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Analytics()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Logs()
        {
            return View();
        }
    }
}

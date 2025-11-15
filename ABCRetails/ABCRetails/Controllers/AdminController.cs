using Microsoft.AspNetCore.Mvc;

namespace ABCRetails.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

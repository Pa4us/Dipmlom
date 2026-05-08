using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    public class TestConroller : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

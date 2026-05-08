using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPP.Controllers;

public class HomeController : Controller
{
    [Authorize]
    public IActionResult Index()
    {
        if (User.IsInRole("Educator"))  return RedirectToAction("Dashboard", "Educator");
        if (User.IsInRole("Inspector")) return RedirectToAction("Dashboard", "Inspector");
        if (User.IsInRole("Mechanic"))  return RedirectToAction("Dashboard", "Mechanic");
        return RedirectToAction("Dashboard", "Student");
    }

    public IActionResult Error() => View();
}

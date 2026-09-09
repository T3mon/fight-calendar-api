using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FightCalendar.Web.Models;

namespace FightCalendar.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // This is an API-only project; the root path has no real content to
    // show. Redirecting to /api/health means hitting the bare domain
    // immediately tells you which environment you're looking at, instead
    // of the unbranded default MVC template page.
    public IActionResult Index()
    {
        return Redirect("/api/health");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

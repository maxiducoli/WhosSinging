using Microsoft.AspNetCore.Mvc;
using WhosSinging.Web.Models;
using WhosSinging.Web.Services;

namespace WhosSinging.Web.Controllers;

public class HomeController : Controller
{
    private readonly ISongService _songService;

    public HomeController(ISongService songService)
    {
        _songService = songService;
    }

    public async Task<IActionResult> Index()
    {
        var songs = await _songService.GetActiveSongsAsync();
        return View(songs);
    }
}
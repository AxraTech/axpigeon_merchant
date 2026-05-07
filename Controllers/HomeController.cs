using AxpigeonApp.Models;
using AxpigeonApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Security.Claims;

namespace AxpigeonApp.Controllers;

public class HomeController(IHomeService service) : Controller
{
    private readonly IHomeService _service = service;


    [Authorize(Roles = "MERCHANT")]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        Guid userId = Guid.Parse(User.FindFirstValue("UserId"));
        Console.WriteLine($"Current user id: {userId}");
        var data = await _service.GetData(userId,page, pageSize);
        return View(data);
    }

 

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

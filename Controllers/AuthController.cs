using AxpigeonApp.Models;
using AxpigeonApp.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace AxpigeonApp.Controllers
{
    public class AuthController(IUserService service) : Controller
    {
        private readonly IUserService _service = service;


        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // GET: /Auth/Verification
        [HttpGet]
        public IActionResult Verification()
        {
            return View();
        }

        // GET: /Auth/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _service.Login(email, password);

            if (user == null)
            {
                ViewData["Error"] = "Invalid email or password.";
                return View();
            }

            if (user.status != "ACTIVE")
            {
                ViewData["Error"] = "Your account has been " + (user.status == "SUSPENDED" ? "suspended" : "disabled") + ". Please contact your administrator.";
                return View();
            }

            if (user.merchant_status == "SUSPENDED")
            {
                string reason = !string.IsNullOrEmpty(user.merchant_status_reason)
                    ? $" Reason: {user.merchant_status_reason}" : "";
                ViewData["Error"] = $"Your merchant account has been suspended.{reason} Please contact support.";
                return View();
            }

            if (user.merchant_status == "DISABLED")
            {
                string reason = !string.IsNullOrEmpty(user.merchant_status_reason)
                    ? $" Reason: {user.merchant_status_reason}" : "";
                ViewData["Error"] = $"Your merchant account has been disabled.{reason} Please contact support.";
                return View();
            }

            // Create auth claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.merchant_name ?? "Admin"),
                new Claim("UserId", user.id.ToString()),
                new Claim(ClaimTypes.Role, user.role),
                new Claim("MerchantStatus", user.merchant_status ?? "ACTIVE"),
            };

            var identity = new ClaimsIdentity(claims, "AxPCookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("AxPCookieAuth", principal);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("AxPCookieAuth");
            return RedirectToAction("Login", "Auth");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

using AxpigeonApp.Dto;
using AxpigeonApp.Models;
using AxpigeonApp.Service;
using AxpigeonApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Security.Claims;

namespace AxpigeonApp.Controllers
{
    
    public class MerchantController(IMerchantService service, ILogger<MerchantController> logger) : Controller
    {
        private readonly IMerchantService _service = service;
        private readonly ILogger<MerchantController> _logger = logger;



        [Authorize(Roles = "MERCHANT")]
        // GET: /Merchant
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1,int pageSize = 10)
        {
            // Get current login user id
            Guid userId = Guid.Parse(User.FindFirstValue("UserId"));
            Console.WriteLine($"Current user id: {userId}");
            var merchants = await _service.GetAllMerchantsByMerchant(userId,page, pageSize);
            return View(merchants);
        }

        [Authorize(Roles = "MERCHANT")]
        // GET: /Merchant/ChangePassword
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            return View("ChangePassword");
        }

        [Authorize(Roles = "MERCHANT")]
        // POST: /Merchant/ChangePassword
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string old_password, string new_password, string confirm_password)
        {
            if (string.IsNullOrWhiteSpace(old_password) ||
                string.IsNullOrWhiteSpace(new_password) ||
                string.IsNullOrWhiteSpace(confirm_password))
            {
                _logger.LogWarning("ChangePassword validation failed: missing fields.");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "All password fields are required.";
                return RedirectToAction("ChangePassword", "Merchant");
            }

            try
            {
                // Get current login user id
                var userIdClaim = User.FindFirstValue("UserId");
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogError("ChangePassword failed: invalid UserId claim. ClaimValue={ClaimValue}", userIdClaim);
                    TempData["AlertType"] = "danger";
                    TempData["AlertMessage"] = "Unable to change password. Please sign in again.";
                    return RedirectToAction("ChangePassword", "Merchant");
                }

                _logger.LogInformation("ChangePassword request for UserId={UserId}", userId);
                await _service.ChangePassword(userId, old_password, new_password, confirm_password);
                TempData["AlertType"] = "success";
                TempData["AlertMessage"] = "Password changed successfully.";
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "ChangePassword validation error.");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "ChangePassword unauthorized.");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = ex.Message;
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "ChangePassword user not found.");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangePassword failed with unexpected error.");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "Unable to change password. Please try again.";
            }

            return RedirectToAction("ChangePassword", "Merchant");
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

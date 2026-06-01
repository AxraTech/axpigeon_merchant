using AxpigeonApp.Dto;
using AxpigeonApp.Models;
using AxpigeonApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace AxpigeonApp.Controllers
{
    public class KeyController(IKeyService service, ILogger<KeyController> logger) : Controller
    {
        private readonly IKeyService _service = service;
        private readonly ILogger<KeyController> _logger = logger;

        [Authorize(Roles = "MERCHANT,BRANCH")]
        [HttpGet]
        public async Task<IActionResult> SetupPassphrase()
        {
            var userIdClaim = User.FindFirstValue("UserId");
            if (!Guid.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Auth");

            var branches = await _service.GetBranchesForPassphrase(userId);
            return View(branches);
        }

        [Authorize(Roles = "MERCHANT,BRANCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetupPassphrase(SetupPassphraseDto dto)
        {
            var userIdClaim = User.FindFirstValue("UserId");
            if (!Guid.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Auth");

            try
            {
                await _service.SetupPassphrase(userId, dto);
                TempData["AlertType"] = "success";
                TempData["AlertMessage"] = "Passphrase setup completed successfully.";
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "SetupPassphrase unauthorized for UserId={UserId}", userId);
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "You are not authorized to set up passphrase for this branch.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SetupPassphrase failed for UserId={UserId}", userId);
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "Failed to setup passphrase: " + ex.Message;
            }

            return RedirectToAction("Index", "Home");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

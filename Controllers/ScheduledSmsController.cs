using AxpigeonApp.Dto;
using AxpigeonApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace AxpigeonApp.Controllers
{
    public class ScheduledSmsController(IScheduledSmsService service) : Controller
    {
        private readonly IScheduledSmsService _service = service;

        [Authorize(Roles = "MERCHANT")]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? status = null)
        {
            Guid userId = Guid.Parse(User.FindFirstValue("UserId"));

            var apiPage = page - 1;
            var result = await _service.GetScheduledListAsync(userId, status, apiPage, pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.StatusFilter = status;

            return View(result);
        }

        [Authorize(Roles = "MERCHANT")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            Guid userId = Guid.Parse(User.FindFirstValue("UserId"));
            var brandNames = await _service.GetAllBrandNames(userId);
            return View(brandNames);
        }

        [Authorize(Roles = "MERCHANT")]
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/BulkSMS_Template.xlsx");

            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found");

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BulkSMS_Template.xlsx");
        }

        [Authorize(Roles = "MERCHANT")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] ScheduleMessageDto dto)
        {
            Guid userId = Guid.Parse(User.FindFirstValue("UserId"));

            if (dto.id == Guid.Empty)
                ModelState.AddModelError(nameof(dto.id), "Please select a brand.");

            if (string.IsNullOrWhiteSpace(dto.message))
                ModelState.AddModelError(nameof(dto.message), "Message is required.");

            if (string.IsNullOrWhiteSpace(dto.scheduleDate))
                ModelState.AddModelError(nameof(dto.scheduleDate), "Schedule date is required.");

            if (dto.isBulk)
            {
                if (dto.excelFile == null)
                {
                    ModelState.AddModelError(nameof(dto.excelFile),
                        "Excel file is required for bulk scheduled SMS.");
                }
                else
                {
                    var extension = Path.GetExtension(dto.excelFile.FileName);
                    var allowedExtensions = new[] { ".xlsx", ".xls" };
                    if (string.IsNullOrWhiteSpace(extension) ||
                        !allowedExtensions.Contains(extension.ToLowerInvariant()))
                    {
                        ModelState.AddModelError(nameof(dto.excelFile),
                            "Only .xlsx or .xls files are allowed.");
                    }
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.phone))
                {
                    ModelState.AddModelError(nameof(dto.phone),
                        "Phone number is required for single scheduled SMS.");
                }
                else if (!Regex.IsMatch(dto.phone.Trim(), @"^\d{7,15}$"))
                {
                    ModelState.AddModelError(nameof(dto.phone),
                        "Phone number must contain only digits (7 to 15 digits).");
                }
            }

            if (!ModelState.IsValid)
            {
                var brandNames = await _service.GetAllBrandNames(userId);
                return View(brandNames);
            }

            try
            {
                if (dto.isBulk)
                    await _service.ScheduleBulkAsync(dto);
                else
                    await _service.ScheduleSingleAsync(dto);

                TempData["Success"] = dto.isBulk
                    ? "Bulk SMS scheduled successfully."
                    : "Single SMS scheduled successfully.";

                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty,
                    $"Failed to schedule message: {ex.Message}");
                var brandNames = await _service.GetAllBrandNames(userId);
                return View(brandNames);
            }
        }

        [Authorize(Roles = "MERCHANT")]
        [HttpGet]
        public async Task<IActionResult> Detail(Guid id)
        {
            Guid userId = Guid.Parse(User.FindFirstValue("UserId"));

            try
            {
                var result = await _service.GetScheduleDetailAsync(userId, id);
                return View(result);
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to load schedule details.";
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = "MERCHANT")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(Guid id, string? reason)
        {
            Guid userId = Guid.Parse(User.FindFirstValue("UserId"));

            try
            {
                await _service.CancelScheduleAsync(userId, id, reason);
                TempData["Success"] = "Schedule cancelled successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to cancel schedule: {ex.Message}";
            }

            return RedirectToAction(nameof(Detail), new { id });
        }
    }
}

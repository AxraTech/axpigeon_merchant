using AxpigeonApp.Dto;
using AxpigeonApp.Models;
using AxpigeonApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace AxpigeonApp.Controllers
{
    public class TransactionsController(ITransactionsService service) : Controller
    {

        private readonly ITransactionsService _service = service;

        [Authorize(Roles = "MERCHANT")]
        public async Task<IActionResult> Index(int page = 1,int pageSize = 10)
        {
            Guid userId = Guid.Parse(User.FindFirstValue("UserId"));
            Console.WriteLine($"Current user id: {userId}");
            var transactions = await _service.GetAllTransactions(userId,page, pageSize);
            return View(transactions);
        }

        [Authorize(Roles = "MERCHANT")]
        [HttpGet]
        public async Task<IActionResult> SendMessage()
        {
            Guid userId = Guid.Parse(User.FindFirstValue("UserId"));
            Console.WriteLine($"Current user id: {userId}");
            var brandNames = await _service.GetAllBrandNames(userId);
            return View(brandNames);
        }

        [Authorize(Roles = "MERCHANT")]
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            
            // Path to file in wwwroot
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/BulkSMS_Template.xlsx");

            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found");

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var fileName = "BulkSMS_Template.xlsx";

            // Return the file as download
            return File(fileBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
        }

        [Authorize(Roles = "MERCHANT")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromForm]  SendMessageDto dto)
        {
            Guid userId = Guid.Parse(User.FindFirstValue("UserId"));
            Console.WriteLine($"Current user id: {userId}");
            if (dto.id == Guid.Empty)
            {
                ModelState.AddModelError(nameof(dto.id), "Please select a brand.");
            }

            if (string.IsNullOrWhiteSpace(dto.message))
            {
                ModelState.AddModelError(nameof(dto.message), "Message is required.");
            }

            // Bulk SMS → phone NOT required, excel REQUIRED
            if (dto.isBulk)
            {
                if (dto.excelFile == null)
                {
                    ModelState.AddModelError(nameof(dto.excelFile),
                        "Excel file is required for bulk SMS.");
                }
                else
                {
                    var extension = Path.GetExtension(dto.excelFile.FileName);
                    var allowedExtensions = new[] { ".xlsx", ".xls" };
                    if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension.ToLowerInvariant()))
                    {
                        ModelState.AddModelError(nameof(dto.excelFile), "Only .xlsx or .xls files are allowed.");
                    }
                }
            }
            // Single SMS → phone REQUIRED, excel OPTIONAL
            else
            {
                if (string.IsNullOrWhiteSpace(dto.phone))
                {
                    ModelState.AddModelError(nameof(dto.phone),
                        "Phone number is required for single SMS.");
                }
                else if (!Regex.IsMatch(dto.phone.Trim(), @"^\d{7,15}$"))
                {
                    ModelState.AddModelError(nameof(dto.phone),
                        "Phone number must contain only digits (7 to 15 digits).");
                }
            }


            if (!ModelState.IsValid)
            {
                var invalidBrandNames = await _service.GetAllBrandNames(userId);
                return View(invalidBrandNames);
            }

            try
            {
                if (dto.isBulk)
                {
                    await _service.SendBulkAsync(dto);
                }
                else
                {
                    await _service.sendMessage(dto);
                }

                TempData["Success"] = dto.isBulk
                    ? "Bulk SMS request submitted successfully."
                    : "Single SMS sent successfully.";

                return RedirectToAction(nameof(SendMessage));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                var brandNames = await _service.GetAllBrandNames(userId);
                return View(brandNames);
            }
        }
            


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

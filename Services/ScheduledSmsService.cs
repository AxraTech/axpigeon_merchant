using AxpigeonApp.Dao;
using AxpigeonApp.Dto;
using AxpigeonApp.Repository;
using AxpigeonApp.Utils;
using OfficeOpenXml;
using System.Text.Json;

namespace AxpigeonApp.Services
{
    public class ScheduledSmsService : IScheduledSmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IScheduledSmsRepository _repo;
        private readonly string _apiBaseUrl;

        public ScheduledSmsService(HttpClient httpClient, IScheduledSmsRepository repo, IConfiguration config)
        {
            _httpClient = httpClient;
            _repo = repo;
            _apiBaseUrl = config["AxpigeonApi:BaseUrl"]
                ?? throw new InvalidOperationException("AxpigeonApi:BaseUrl is not configured.");
        }

        public async Task<List<BrandNames>> GetAllBrandNames(Guid userId)
        {
            return await _repo.GetAllBrandNames(userId);
        }

        public async Task ScheduleSingleAsync(ScheduleMessageDto dto)
        {
            var merchant = await _repo.FindValidMerchantAsync(dto.id)
                ?? throw new Exception("Invalid branch or missing credentials.");

            string token = await GetTokenAsync(merchant.username, merchant.password);
            string encryptedMessage = EncryptUtil.EncryptMessage(dto.message, merchant.secret_key);

            var payload = new ScheduleSingleMessageDto
            {
                brandName = merchant.brand_name,
                phone = dto.phone,
                messageHash = encryptedMessage,
                senderId = merchant.sender_id,
                scheduleDate = dto.scheduleDate,
                token = token
            };

            await PostScheduleSingleAsync(payload);
        }

        public async Task ScheduleBulkAsync(ScheduleMessageDto dto)
        {
            var merchant = await _repo.FindValidMerchantAsync(dto.id)
                ?? throw new Exception("Invalid branch or missing credentials.");

            string token = await GetTokenAsync(merchant.username, merchant.password);
            string encryptedMessage = EncryptUtil.EncryptMessage(dto.message, merchant.secret_key);

            List<string> phoneList = new();

            if (dto.excelFile == null || dto.excelFile.Length == 0)
                throw new Exception("Excel file is required for bulk scheduled SMS.");

            using var stream = new MemoryStream();
            await dto.excelFile.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet.Dimension == null)
                throw new Exception("Excel file has no data.");

            int rowCount = worksheet.Dimension.Rows;
            for (int row = 2; row <= rowCount; row++)
            {
                var phone = worksheet.Cells[row, 1].Text?.Trim();
                if (!string.IsNullOrEmpty(phone))
                    phoneList.Add(phone);
            }

            phoneList = phoneList.Distinct().ToList();

            if (phoneList.Count == 0)
                throw new Exception("No valid phone numbers found in Excel file.");

            var payload = new ScheduleBulkMessageDto
            {
                brandName = merchant.brand_name,
                listPhoneNumber = phoneList,
                messageHash = encryptedMessage,
                senderId = merchant.sender_id,
                scheduleDate = dto.scheduleDate,
                token = token
            };

            await PostScheduleBulkAsync(payload);
        }

        public async Task<ScheduledSmsListResponseDao> GetScheduledListAsync(
            Guid userId, string? status, int page, int size)
        {
            var brandNames = await _repo.GetAllBrandNames(userId);
            if (brandNames.Count == 0)
                return new ScheduledSmsListResponseDao
                {
                    status = "00",
                    message = "Success",
                    totalCount = 0,
                    items = new List<ScheduledSmsDataDao>()
                };

            var firstBrand = brandNames.First();
            string token = await GetTokenAsync(firstBrand.username, firstBrand.password);

            var url = $"{_apiBaseUrl}/api/sms/schedule?page={page}&size={size}";
            if (!string.IsNullOrWhiteSpace(status))
                url += $"&status={status}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            var rawResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to fetch scheduled SMS list: {rawResponse}");

            var result = JsonSerializer.Deserialize<ScheduledSmsListResponseDao>(rawResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result ?? new ScheduledSmsListResponseDao
            {
                status = "00",
                message = "Success",
                totalCount = 0,
                items = new List<ScheduledSmsDataDao>()
            };
        }

        public async Task<ScheduledSmsResponseDao> GetScheduleDetailAsync(Guid userId, Guid scheduleId)
        {
            var brandNames = await _repo.GetAllBrandNames(userId);
            if (brandNames.Count == 0)
                throw new Exception("No brands found for this user.");

            var firstBrand = brandNames.First();
            string token = await GetTokenAsync(firstBrand.username, firstBrand.password);

            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_apiBaseUrl}/api/sms/schedule/{scheduleId}");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            var rawResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to fetch schedule detail: {rawResponse}");

            var result = JsonSerializer.Deserialize<ScheduledSmsResponseDao>(rawResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result ?? throw new Exception("Schedule detail response is null.");
        }

        public async Task<ScheduledSmsResponseDao> CancelScheduleAsync(
            Guid userId, Guid scheduleId, string? reason)
        {
            var brandNames = await _repo.GetAllBrandNames(userId);
            if (brandNames.Count == 0)
                throw new Exception("No brands found for this user.");

            var firstBrand = brandNames.First();
            string token = await GetTokenAsync(firstBrand.username, firstBrand.password);

            var url = $"{_apiBaseUrl}/api/sms/schedule/{scheduleId}/cancel";
            if (!string.IsNullOrWhiteSpace(reason))
                url += $"?reason={Uri.EscapeDataString(reason)}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            var rawResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to cancel schedule: {rawResponse}");

            var result = JsonSerializer.Deserialize<ScheduledSmsResponseDao>(rawResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result ?? throw new Exception("Cancel response is null.");
        }

        private async Task PostScheduleSingleAsync(ScheduleSingleMessageDto dto)
        {
            var requestBody = new
            {
                brandName = dto.brandName,
                phone = dto.phone,
                messageHash = dto.messageHash,
                senderId = dto.senderId,
                scheduleDate = dto.scheduleDate,
            };

            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_apiBaseUrl}/api/sms/schedule");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", dto.token);
            request.Content = JsonContent.Create(requestBody);

            var response = await _httpClient.SendAsync(request);
            var rawResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Schedule single SMS failed: {rawResponse}");

            var apiResponse = JsonSerializer.Deserialize<ScheduledSmsResponseDao>(rawResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (apiResponse == null)
                throw new Exception("Schedule response is null.");

            if (apiResponse.status != "00")
                throw new Exception(apiResponse.message);
        }

        private async Task PostScheduleBulkAsync(ScheduleBulkMessageDto dto)
        {
            var requestBody = new
            {
                brandName = dto.brandName,
                listPhoneNumber = dto.listPhoneNumber,
                messageHash = dto.messageHash,
                senderId = dto.senderId,
                scheduleDate = dto.scheduleDate,
            };

            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_apiBaseUrl}/api/sms/schedule/bulk");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", dto.token);
            request.Content = JsonContent.Create(requestBody);

            var response = await _httpClient.SendAsync(request);
            var rawResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Schedule bulk SMS failed: {rawResponse}");

            var apiResponse = JsonSerializer.Deserialize<ScheduledSmsResponseDao>(rawResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (apiResponse == null)
                throw new Exception("Schedule response is null.");

            if (apiResponse.status != "00")
                throw new Exception(apiResponse.message);
        }

        private async Task<string> GetTokenAsync(string username, string password)
        {
            var requestBody = new { username, password };
            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiBaseUrl}/api/auth/login", requestBody);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Auth API failed.");

            var authResponse = await response.Content.ReadFromJsonAsync<AuthTokenResponseDao>();

            if (authResponse == null)
                throw new Exception("Auth response is null.");
            if (authResponse.status != "000")
                throw new Exception($"Auth failed: {authResponse.message}");
            if (string.IsNullOrWhiteSpace(authResponse.data))
                throw new Exception("Token is empty.");

            return authResponse.data;
        }
    }
}

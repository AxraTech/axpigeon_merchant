using AxpigeonApp.Dao;
using AxpigeonApp.Dto;
using AxpigeonApp.Repository;
using AxpigeonApp.Utils;
using OfficeOpenXml;
using System.Text.Json;



namespace AxpigeonApp.Services
{
    public class TransactionsService : ITransactionsService
    {
        private readonly HttpClient _httpClient;
        private readonly ITransactionsRepository _repo;
        private readonly string _apiBaseUrl;

        public TransactionsService(HttpClient httpClient, ITransactionsRepository repo, IConfiguration config)
        {
            _httpClient = httpClient;
            _repo = repo;
            _apiBaseUrl = config["AxpigeonApi:BaseUrl"]
                ?? throw new InvalidOperationException("AxpigeonApi:BaseUrl is not configured.");
            if (string.IsNullOrWhiteSpace(_apiBaseUrl))
                throw new InvalidOperationException("AxpigeonApi:BaseUrl is not configured.");
        }

        public async Task<PaginatedList<TransactionListDao>> GetAllTransactions(Guid userId, int page, int pageSize)
        {
            var dao = await _repo.GetAllTransactions(userId,page, pageSize);
            var mappedItems = dao.Items.Select(x => new TransactionListDao
            {
                id = x.id,
                transactionId = x.transactionId,
                brand_name = x.brand_name,
                merchant_name = x.merchant_name,
                message = x.message,
                operator_name = x.operator_name,
                pov_transaction_id = x.pov_transaction_id,
                schedule_date = x.schedule_date,
                sent_at = x.sent_at,
                sms_type = x.sms_type,
                provider_name = x.provider_name,
                pov_campaign_id = x.pov_campaign_id,
                transaction_id = x.transaction_id,
                is_send_now = x.is_send_now,
                transaction_tmp_id = x.transaction_tmp_id,
                status = x.status,
                created_at = x.created_at,
                phone = x.phone,
                msgPassword = x.msgPassword,
            }).ToList();
            // Return as PaginatedList<TransactionListDao>
            return new PaginatedList<TransactionListDao>
            {
                Items = mappedItems,
                TotalCount = dao.TotalCount,
                Page = dao.Page,
                PageSize = dao.PageSize
            };
        }
        public async Task<List<BrandNames>> GetAllBrandNames(Guid userId)
        {
            var dao = await _repo.GetAllBrandNames(userId);
            var mappedItems = dao.Select(x => new BrandNames
            {
                id = x.id,
                brand_name = x.brand_name,
                username = x.username,
                password = x.password,
                
            }).ToList();
            return mappedItems;
        }

        public async Task SendBulkAsync(SendMessageDto dto)
        {
            ValidateMerchantDao merchant = await _repo.FindValidMerchantAsync(dto);
            List<string> phoneList = new();

            string token = await GetTokenAsync(merchant.username, merchant.password);

            Console.WriteLine($"Brand      : {merchant.brand_name}");
            Console.WriteLine($"Sender ID  : {merchant.sender_id}");
            Console.WriteLine($"Token(end) : {token[^6..]}");

            string encryptedMessage = EncryptUtil.EncryptMessage(
                dto.message,
                merchant.secret_key
            );

            if (!dto.isBulk)
                throw new Exception("Bulk flag is false");

            if (dto.excelFile == null || dto.excelFile.Length == 0)
                throw new Exception("Excel file is required for bulk SMS");


            using var stream = new MemoryStream();
            await dto.excelFile.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet.Dimension == null)
                throw new Exception("Excel file has no data");

            int rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                var phone = worksheet.Cells[row, 1].Text?.Trim();
                if (!string.IsNullOrEmpty(phone))
                    phoneList.Add(phone);
            }

            // Remove duplicates
            phoneList = phoneList.Distinct().ToList();

            Console.WriteLine($"📞 Total phones: {phoneList.Count}");

            var sendMessage = new SendBulkMessageDto
            {
                sender_id = merchant.sender_id,
                brand_name = merchant.brand_name,
                listPhoneNumber = phoneList,
                message = encryptedMessage,
                is_send_now = true,
                schedule_date = null,
                token = token
            };

            Console.WriteLine("===== BULK SMS PAYLOAD =====");
            Console.WriteLine(JsonSerializer.Serialize(
                sendMessage,
                new JsonSerializerOptions { WriteIndented = true }
            ));
            Console.WriteLine("============================");

            await SendBulkMessage(sendMessage);
        }

        public async Task sendMessage(SendMessageDto dto)
        {
            ValidateMerchantDao merchant = await _repo.FindValidMerchantAsync(dto);

            if (dto.isBulk)
                throw new Exception("Bulk flag is true");

            string token = await GetTokenAsync(merchant.username, merchant.password);

            string encryptedMessage = EncryptUtil.EncryptMessage(
                dto.message,
                merchant.secret_key
            );

            var sendMessage = new SendSingleMessageDto
            {
                sender_id = merchant.sender_id,
                brand_name = merchant.brand_name,
                phone = dto.phone,
                message = encryptedMessage,
                is_send_now = true,
                schedule_date = null,
                token = token
            };

            await SendSingleMessage(sendMessage);
        }

      private async Task<string> SendBulkMessage(SendBulkMessageDto dto)
        {
            Console.WriteLine("=== SendSingleMessage START ===");
            Console.WriteLine($"Brand       : {dto.brand_name}");
            Console.WriteLine($"SenderId    : {dto.sender_id}");
            Console.WriteLine($"Send Now    : {dto.is_send_now}");
            Console.WriteLine($"Schedule    : {dto.schedule_date}");
            Console.WriteLine($"Token (end) : {(dto.token?.Length > 6 ? dto.token[^6..] : "N/A")}");

            var requestBody = new
            {
                brandName = dto.brand_name,
                listPhoneNumber = dto.listPhoneNumber,
                messageHash = dto.message,
                senderId = dto.sender_id,
                isSendNow = dto.is_send_now,
                scheduleDate = dto.schedule_date,
            };

            Console.WriteLine("Request Body:");
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(requestBody));

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_apiBaseUrl}/api/sms/bulk"
            );

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    dto.token
                );

            request.Content = JsonContent.Create(requestBody);

            HttpResponseMessage response;
            try
            {
                Console.WriteLine("Sending HTTP request...");
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ HTTP request exception");
                Console.WriteLine(ex.ToString());
                throw;
            }

            Console.WriteLine($"HTTP Status Code : {(int)response.StatusCode} ({response.StatusCode})");

            var rawResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Raw API Response:");
            Console.WriteLine(rawResponse);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("❌ SMS send failed");
                throw new Exception($"Send SMS failed: {rawResponse}");
            }

            var apiResponse =
                System.Text.Json.JsonSerializer.Deserialize<SingleMessageRespDao>(rawResponse);

            if (apiResponse == null)
            {
                Console.WriteLine("❌ API response is NULL");
                throw new Exception("Send message response is null");
            }

            Console.WriteLine($"API Status  : {apiResponse.status}");
            Console.WriteLine($"API Message : {apiResponse.message}");

            if (apiResponse.status != "00")
            {
                Console.WriteLine("⚠️ SMS API returned non-success status");
                throw new Exception(apiResponse.message);
            }

            Console.WriteLine("✅ SMS sent successfully");
            Console.WriteLine("=== SendSingleMessage END ===");

            return apiResponse.message;

        }

      private async Task<string> SendSingleMessage(SendSingleMessageDto dto)
        {
            Console.WriteLine("=== SendSingleMessage START ===");
            Console.WriteLine($"Brand       : {dto.brand_name}");
            Console.WriteLine($"Phone       : {dto.phone}");
            Console.WriteLine($"SenderId    : {dto.sender_id}");
            Console.WriteLine($"Send Now    : {dto.is_send_now}");
            Console.WriteLine($"Schedule    : {dto.schedule_date}");
            Console.WriteLine($"Token (end) : {(dto.token?.Length > 6 ? dto.token[^6..] : "N/A")}");

            var requestBody = new
            {
                brandName = dto.brand_name,
                phone = dto.phone,
                messageHash = dto.message,
                senderId = dto.sender_id,
                isSendNow = dto.is_send_now,
                scheduleDate = dto.schedule_date,
            };

            Console.WriteLine("Request Body:");
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(requestBody));

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_apiBaseUrl}/api/sms/send"
            );

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    dto.token
                );

            request.Content = JsonContent.Create(requestBody);

            HttpResponseMessage response;

            try
            {
                Console.WriteLine("Sending HTTP request...");
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ HTTP request exception");
                Console.WriteLine(ex.ToString());
                throw;
            }

            Console.WriteLine($"HTTP Status Code : {(int)response.StatusCode} ({response.StatusCode})");

            var rawResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Raw API Response:");
            Console.WriteLine(rawResponse);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("❌ SMS send failed");
                throw new Exception($"Send SMS failed: {rawResponse}");
            }

            var apiResponse =
                System.Text.Json.JsonSerializer.Deserialize<SingleMessageRespDao>(rawResponse);

            if (apiResponse == null)
            {
                Console.WriteLine("❌ API response is NULL");
                throw new Exception("Send message response is null");
            }

            Console.WriteLine($"API Status  : {apiResponse.status}");
            Console.WriteLine($"API Message : {apiResponse.message}");

            if (apiResponse.status != "00")
            {
                Console.WriteLine("⚠️ SMS API returned non-success status");
                throw new Exception(apiResponse.message);
            }

            Console.WriteLine("✅ SMS sent successfully");
            Console.WriteLine("=== SendSingleMessage END ===");

            return apiResponse.message;
}

      private async Task<string> GetTokenAsync(string username, string password)
        {
            var requestBody = new
            {
                username = username,
                password = password
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiBaseUrl}/api/auth/login",
                requestBody
            );

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Auth API failed");
            }

            var authResponse =
                await response.Content.ReadFromJsonAsync<AuthTokenResponseDao>();

            if (authResponse == null)
                throw new Exception("Auth response is null");

            if (authResponse.status != "000")
                throw new Exception($"Auth failed: {authResponse.message}");

            if (string.IsNullOrWhiteSpace(authResponse.data))
                throw new Exception("Token is empty");

            return authResponse.data; // ✅ JWT token
        }


    }
}

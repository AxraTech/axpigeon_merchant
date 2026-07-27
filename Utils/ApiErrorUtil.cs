using System.Text.Json;
using AxpigeonApp.Dao;

namespace AxpigeonApp.Utils
{
    public static class ApiErrorUtil
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static string ExtractMessage(string rawBody, string fallback)
        {
            if (string.IsNullOrWhiteSpace(rawBody))
                return fallback;

            try
            {
                var err = JsonSerializer.Deserialize<ApiErrorResponseDao>(rawBody, JsonOptions);
                if (err == null || string.IsNullOrWhiteSpace(err.message))
                    return fallback;

                return err.error switch
                {
                    "04" => $"Account inactive: {err.message}",
                    "07" => $"Merchant account suspended: {err.message}",
                    "08" => $"Merchant account disabled: {err.message}",
                    "09" => $"Branch suspended or disabled: {err.message}",
                    "11" => $"Account is read-only: {err.message}",
                    "16" => $"Rate limit exceeded: {err.message}",
                    "10" => err.message,
                    "05" => err.message,
                    _ => err.message
                };
            }
            catch
            {
                return fallback;
            }
        }
    }
}

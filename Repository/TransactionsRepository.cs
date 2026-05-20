using AxpigeonApp.Dao;
using AxpigeonApp.Dto;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
namespace AxpigeonApp.Repository
{
    public class TransactionsRepository : ITransactionsRepository
    {
        private readonly IConfiguration _config;
        private readonly string _conn;

        public TransactionsRepository(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _conn = _config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

       
        public async Task<PaginatedList<TransactionListDao>> GetAllTransactions(Guid userId,int page, int pageSize)
        {
            var result = new PaginatedList<TransactionListDao>();
            var items = new List<TransactionListDao>();
            Guid merchantId = Guid.Empty;

            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            // 1️⃣ Get merchant_id of logged in user
            using (var merchantIdCmd = new NpgsqlCommand(
                @"SELECT merchant_id 
          FROM tbl_users 
          WHERE id = @userId;", con))
            {
                merchantIdCmd.Parameters.AddWithValue("@userId", userId);

                var obj = await merchantIdCmd.ExecuteScalarAsync();

                if (obj != null && obj != DBNull.Value)
                    merchantId = (Guid)obj;
                else
                    return result; // user not linked to merchant
            }

            // Count Query
            using (var countCmd = new NpgsqlCommand(
                @"SELECT COUNT(*) FROM tbl_transactions t  WHERE t.merchant_id = @merchant_id", con))
            {
                countCmd.Parameters.AddWithValue("@merchant_id", merchantId);
                result.TotalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }
            using var cmd = new NpgsqlCommand(@"
            SELECT 
                t.id, 
                t.is_send_now, 
                t.message, 
                t.operator_name, 
                t.pov_campaign_id, 
                t.pov_transaction_id, 
                t.schedule_date, 
                t.sent_at, 
                t.sms_type, 
                t.transaction_id, 
                t.transaction_tmp_id, 
                t.status, 
                t.created_at, 
                b.brand_name,
                m.name AS merchant_name,
                p.name AS provider_name,
                t.phone,
                cdl.secret_key,
                msgd.password AS msgPassword
            FROM tbl_transactions t
            LEFT JOIN tbl_branches b ON t.branch_id = b.id
            LEFT JOIN tbl_users u ON b.id = u.branch_id
            LEFT JOIN tbl_merchants m ON t.merchant_id = m.id
            LEFT JOIN tbl_provider p ON b.provider_id = p.id
            LEFT JOIN tbl_message_decrypt msgd ON u.msg_decrypt_id = msgd.id
            LEFT JOIN tbl_credentials cdl ON b.credential_id = cdl.id
            WHERE t.merchant_id = @merchant_id
            ORDER BY t.created_at DESC
            LIMIT @pageSize OFFSET @offset;
        ", con);
            cmd.Parameters.AddWithValue("@merchant_id", merchantId);
            cmd.Parameters.AddWithValue("@pageSize", pageSize);
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new TransactionListDao
                {
                    id = reader.GetGuid(0),

                    is_send_now = reader.IsDBNull(1) ? true : reader.GetBoolean(1),
                    message = reader.IsDBNull(2) ? "" : DecryptMessage(reader.GetString(17),reader.GetString(2)),
                    operator_name = reader.IsDBNull(3) ? "" : reader.GetString(3),

                    pov_campaign_id = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    pov_transaction_id = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    schedule_date = reader.IsDBNull(6) ? "" : reader.GetDateTime(6).ToString("yyyy-MM-dd HH:mm:ss"),

                    sent_at = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7),
                    sms_type = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    transaction_id = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    transaction_tmp_id = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    status = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    created_at = reader.IsDBNull(12) ? DateTime.MinValue : reader.GetDateTime(12),
                    brand_name = reader.IsDBNull(13) ? "" : reader.GetString(13),
                    merchant_name = reader.IsDBNull(14) ? "" : reader.GetString(14),
                    provider_name = reader.IsDBNull(15) ? "" : reader.GetString(15),
                    phone = reader.IsDBNull(16) ? "" : reader.GetString(16),
                    msgPassword = reader.IsDBNull(18) ? "" : DecryptMessage(reader.GetString(17),reader.GetString(18))
                });
            }

            result.Items = items;
            result.Page = page;
            result.PageSize = pageSize;

            return result;
        }

        // Format key like your KeyFormatService in Java
        private static byte[] FormatKey(string secretKey)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] formattedKey = new byte[32]; // AES-256
            int len = Math.Min(keyBytes.Length, 32);
            Array.Copy(keyBytes, formattedKey, len);
            return formattedKey;
        }

        // Format IV like your KeyFormatService in Java
        private static byte[] FormatIV(string secretKey)
        {
            byte[] ivBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] formattedIV = new byte[16]; // AES block size
            int len = Math.Min(ivBytes.Length, 16);
            Array.Copy(ivBytes, formattedIV, len);
            return formattedIV;
        }
        public static string DecryptMessage(string secretKey, string base64Message)
        {
            try
            {
                byte[] key = FormatKey(secretKey);
                byte[] iv = FormatIV(secretKey);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7; // PKCS5 in Java = PKCS7 in .NET

                    byte[] encryptedBytes = Convert.FromBase64String(base64Message);

                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid secret key or message", ex);
            }
        }

        public async Task<List<BrandNames>> GetAllBrandNames(Guid userId)
        {
            var brandNames = new List<BrandNames>();
            Guid merchantId = Guid.Empty;
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            // 1️⃣ Get merchant_id of logged in user
            using (var merchantIdCmd = new NpgsqlCommand(
                @"SELECT merchant_id 
              FROM tbl_users 
              WHERE id = @userId;", con))
                {
                    merchantIdCmd.Parameters.AddWithValue("@userId", userId);

                    var obj = await merchantIdCmd.ExecuteScalarAsync();

                    if (obj != null && obj != DBNull.Value)
                        merchantId = (Guid)obj;
                    else
                        return brandNames; // user not linked to merchant
                }
            using var cmd = new NpgsqlCommand(@"
            SELECT
                b.id,
                b.brand_name,
                gw.username,
                gw.pass_text AS password
            FROM tbl_branches b
            LEFT JOIN tbl_users u ON b.id = u.branch_id
            LEFT JOIN tbl_api_gateway gw ON u.api_gw_id = gw.id
            WHERE b.merchant_id = @merchant_id
        ", con);

            cmd.Parameters.AddWithValue("@merchant_id", merchantId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                brandNames.Add(new BrandNames
                {
                    id = reader.GetGuid(0),
                    brand_name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    username = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    password = reader.IsDBNull(3) ? "" : reader.GetString(3),
                });
            }
            return brandNames;
        }

        public async Task<ValidateMerchantDao?> FindValidMerchantAsync(SendMessageDto dto)
        {
            ValidateMerchantDao? merchant = null;

            await using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT gw.username, gw.pass_text AS password, b.brand_name, cdl.sender_id, cdl.secret_key " +
                "FROM tbl_users u " +
                "LEFT JOIN tbl_branches b ON u.branch_id = b.id " +
                "LEFT JOIN tbl_credentials cdl ON b.credential_id = cdl.id " +
                "LEFT JOIN tbl_api_gateway gw ON u.api_gw_id = gw.id " +
                "WHERE u.branch_id = @id",
                con
            );

            cmd.Parameters.AddWithValue("id", dto.id);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                merchant = new ValidateMerchantDao
                {
                    username = reader.IsDBNull(0) ? null : reader.GetString(0),
                    password = reader.IsDBNull(1) ? null : reader.GetString(1),
                    brand_name = reader.IsDBNull(2) ? null : reader.GetString(2),
                    sender_id = reader.IsDBNull(3) ? null : reader.GetString(3),
                    secret_key = reader.IsDBNull(4) ? null : reader.GetString(4)
                };
            }

            return merchant;
        }


        
    }
}

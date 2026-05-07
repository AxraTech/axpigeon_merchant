using AxpigeonApp.Dao;
using Npgsql;
using System.Security.Cryptography;
using System.Text;

namespace AxpigeonApp.Repository
{
    public class HomeRepository : IHomeRepository
    {
        private readonly IConfiguration _config;
        private readonly string _conn;

        public HomeRepository(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _conn = _config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        //Get All Recent Transaction
        public async Task<DashboardDao> GetData(Guid userId, int page, int pageSize)
        {
            var result = new DashboardDao();
            Guid merchantId = Guid.Empty;

            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            // =====================================================
            // 1️⃣ Get merchant_id of logged in user
            // =====================================================
            using (var merchantIdCmd = new NpgsqlCommand(@"
        SELECT merchant_id
        FROM tbl_users
        WHERE id = @userId;", con))
            {
                merchantIdCmd.Parameters.AddWithValue("@userId", userId);

                var obj = await merchantIdCmd.ExecuteScalarAsync();

                if (obj == null || obj == DBNull.Value)
                    return result;

                merchantId = (Guid)obj;
            }

            // =====================================================
            // 2️⃣ TODAY TRANSACTION COUNT (SECURE)
            // =====================================================
            using (var countCmd = new NpgsqlCommand(@"
        SELECT COUNT(*)
        FROM tbl_transactions t
        WHERE t.merchant_id = @merchant_id
          AND t.created_at >= CURRENT_DATE
          AND t.created_at < CURRENT_DATE + INTERVAL '1 day';", con))
            {
                countCmd.Parameters.AddWithValue("@merchant_id", merchantId);

                result.transactions.TotalCount =
                    Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            // =====================================================
            // 3️⃣ SMS PACKAGE LIST (READER CLOSED PROPERLY)
            // =====================================================
            using (var cmdPackageList = new NpgsqlCommand(@"
        SELECT 
            snf.mpt,
            snf.mytel,
            snf.u9,
            snf.atom,
            snf.mec,
            snf.purchase_atom,
            snf.purchase_mpt,
            snf.purchase_u9,
            snf.purchase_mytel,
            snf.purchase_mec
        FROM tbl_branches b
        JOIN tbl_users u 
            ON b.id = u.branch_id 
           AND u.role = 'BRANCH'
           AND u.merchant_id = @merchant_id
        LEFT JOIN tbl_sms_info snf ON b.sms_info_id = snf.id;", con))
            {
                cmdPackageList.Parameters.AddWithValue("@merchant_id", merchantId);

                using (var reader = await cmdPackageList.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.packageList.Add(new DashboardSmsPackages
                        {
                            mpt = reader.IsDBNull(0) ? "" : reader.GetString(0),
                            mytel = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            u9 = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            atom = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            mec = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            purchase_atom = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            purchase_mpt = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            purchase_u9 = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            purchase_mytel = reader.IsDBNull(8) ? "" : reader.GetString(8),
                            purchase_mec = reader.IsDBNull(9) ? "" : reader.GetString(9),
                        });
                    }
                } // ⭐ reader CLOSED here
            }

            // =====================================================
            // 4️⃣ TRANSACTION LIST (SECURE + PAGINATION)
            // =====================================================
            using (var cmd = new NpgsqlCommand(@"
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
            msgd.password
        FROM tbl_transactions t
        LEFT JOIN tbl_branches b ON t.branch_id = b.id
        LEFT JOIN tbl_merchants m ON t.merchant_id = m.id
        LEFT JOIN tbl_users u ON b.id = u.branch_id
        LEFT JOIN tbl_message_decrypt msgd ON u.msg_decrypt_id = msgd.id
        LEFT JOIN tbl_credentials cdl ON b.credential_id = cdl.id
        LEFT JOIN tbl_provider p ON b.provider_id = p.id
        WHERE t.merchant_id = @merchant_id
          AND t.created_at >= CURRENT_DATE
          AND t.created_at < CURRENT_DATE + INTERVAL '1 day'
        ORDER BY t.created_at DESC
        LIMIT @pageSize OFFSET @offset;", con))
            {
                cmd.Parameters.AddWithValue("@merchant_id", merchantId);
                cmd.Parameters.AddWithValue("@pageSize", pageSize);
                cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var secretKey = reader.IsDBNull(17) ? "" : reader.GetString(17);
                    var encryptedMsg = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    var encryptedPwd = reader.IsDBNull(18) ? "" : reader.GetString(18);

                    result.transactions.Items.Add(new TransactionListDao
                    {
                        id = reader.GetGuid(0),
                        is_send_now = reader.IsDBNull(1) || reader.GetBoolean(1),
                        message = encryptedMsg == "" ? "" : DecryptMessage(secretKey, encryptedMsg),
                        operator_name = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        pov_campaign_id = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        pov_transaction_id = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        schedule_date = reader.IsDBNull(6) ? "" : reader.GetString(6),
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
                        msgPassword = encryptedPwd == "" ? "" : DecryptMessage(secretKey, encryptedPwd),
                    });
                }
            }

            result.transactions.Page = page;
            result.transactions.PageSize = pageSize;

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

    }
}

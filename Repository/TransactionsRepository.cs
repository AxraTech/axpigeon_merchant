using AxpigeonApp.Dao;
using AxpigeonApp.Dto;
using AxpigeonApp.Utils;
using Npgsql;

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
                    return result;
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
                cdl.key_version,
                msgd.merchant_wrapped_dek,
                msgd.dek_salt,
                msgd.dek_iv,
                CASE WHEN msgd.merchant_wrapped_dek IS NOT NULL THEN true ELSE false END AS has_passphrase
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
                    message = "***encrypted***",
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
                    encryptedMessage = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    keyVersion = reader.IsDBNull(17) ? 1 : reader.GetInt32(17),
                    wrappedDek = reader.IsDBNull(18) ? "" : reader.GetString(18),
                    dekSalt = reader.IsDBNull(19) ? "" : reader.GetString(19),
                    dekIv = reader.IsDBNull(20) ? "" : reader.GetString(20),
                    hasPassphrase = reader.IsDBNull(21) ? false : reader.GetBoolean(21),
                    msgPassword = "",
                });
            }

            result.Items = items;
            result.Page = page;
            result.PageSize = pageSize;

            return result;
        }

        public async Task<List<BrandNames>> GetAllBrandNames(Guid userId)
        {
            var brandNames = new List<BrandNames>();
            Guid merchantId = Guid.Empty;
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

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
                        return brandNames;
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
                "SELECT gw.username, gw.pass_text AS password, b.brand_name, cdl.sender_id, " +
                "cdl.secret_key, cdl.encrypted_secret_key, cdl.key_version " +
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
                int keyVersion = reader.IsDBNull(6) ? 1 : reader.GetInt32(6);
                string secretKey;

                if (keyVersion >= 2 && !reader.IsDBNull(5))
                {
                    secretKey = ServerKeyUtil.UnwrapDek(reader.GetString(5));
                }
                else
                {
                    secretKey = reader.IsDBNull(4) ? null : reader.GetString(4);
                }

                merchant = new ValidateMerchantDao
                {
                    username = reader.IsDBNull(0) ? null : reader.GetString(0),
                    password = reader.IsDBNull(1) ? null : reader.GetString(1),
                    brand_name = reader.IsDBNull(2) ? null : reader.GetString(2),
                    sender_id = reader.IsDBNull(3) ? null : reader.GetString(3),
                    secret_key = secretKey
                };
            }

            return merchant;
        }


        
    }
}

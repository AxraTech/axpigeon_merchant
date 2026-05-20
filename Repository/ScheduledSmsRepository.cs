using AxpigeonApp.Dao;
using Npgsql;

namespace AxpigeonApp.Repository
{
    public class ScheduledSmsRepository : IScheduledSmsRepository
    {
        private readonly string _conn;

        public ScheduledSmsRepository(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<List<BrandNames>> GetAllBrandNames(Guid userId)
        {
            var brandNames = new List<BrandNames>();
            Guid merchantId = Guid.Empty;

            await using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            await using (var merchantIdCmd = new NpgsqlCommand(
                @"SELECT merchant_id FROM tbl_users WHERE id = @userId;", con))
            {
                merchantIdCmd.Parameters.AddWithValue("@userId", userId);
                var obj = await merchantIdCmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value)
                    merchantId = (Guid)obj;
                else
                    return brandNames;
            }

            await using var cmd = new NpgsqlCommand(@"
                SELECT b.id, b.brand_name, gw.username, gw.pass_text AS password
                FROM tbl_branches b
                LEFT JOIN tbl_users u ON b.id = u.branch_id
                LEFT JOIN tbl_api_gateway gw ON u.api_gw_id = gw.id
                WHERE b.merchant_id = @merchant_id", con);

            cmd.Parameters.AddWithValue("@merchant_id", merchantId);
            await using var reader = await cmd.ExecuteReaderAsync();

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

        public async Task<ValidateMerchantDao?> FindValidMerchantAsync(Guid branchId)
        {
            ValidateMerchantDao? merchant = null;

            await using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                @"SELECT gw.username, gw.pass_text AS password, b.brand_name, cdl.sender_id, cdl.secret_key
                  FROM tbl_users u
                  LEFT JOIN tbl_branches b ON u.branch_id = b.id
                  LEFT JOIN tbl_credentials cdl ON b.credential_id = cdl.id
                  LEFT JOIN tbl_api_gateway gw ON u.api_gw_id = gw.id
                  WHERE u.branch_id = @id", con);

            cmd.Parameters.AddWithValue("id", branchId);
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

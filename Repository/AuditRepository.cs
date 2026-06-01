using Npgsql;

namespace AxpigeonApp.Repository
{
    public class AuditRepository : IAuditRepository
    {
        private readonly string _conn;

        public AuditRepository(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task LogDecryptAttempt(Guid userId, Guid? branchId, string action, string? ipAddress, string? userAgent)
        {
            await using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO tbl_decrypt_audit_log (id, user_id, branch_id, action, ip_address, user_agent, created_at)
                VALUES (gen_random_uuid(), @user_id, @branch_id, @action, @ip, @ua, NOW())", con);

            cmd.Parameters.AddWithValue("@user_id", userId);
            cmd.Parameters.AddWithValue("@branch_id", branchId.HasValue ? branchId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@action", action);
            cmd.Parameters.AddWithValue("@ip", (object?)ipAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ua", (object?)userAgent ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}

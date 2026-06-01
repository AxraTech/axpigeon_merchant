using AxpigeonApp.Dao;
using AxpigeonApp.Dto;
using AxpigeonApp.Utils;
using Npgsql;

namespace AxpigeonApp.Repository
{
    public class KeyRepository : IKeyRepository
    {
        private readonly IConfiguration _config;
        private readonly string _conn;

        public KeyRepository(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _conn = _config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<List<BrandNames>> GetAllBrandNames()
        {
            var brandNames = new List<BrandNames>();
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
            SELECT
                b.id,
                b.brand_name,
                gw.username,
                gw.pass_text AS password
            FROM tbl_branches b
            LEFT JOIN tbl_users u ON b.id = u.branch_id
            LEFT JOIN tbl_api_gateway gw ON u.api_gw_id = gw.id
            WHERE u.msg_decrypt_id IS NULL
            AND u.api_gw_id IS NOT NULL;
        ", con);

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

        //Get All keys By Admin
        public async Task<PaginatedList<KeyListDao>> GetAllKeys(int page, int pageSize)
        {
            var result = new PaginatedList<KeyListDao>();
            var items = new List<KeyListDao>();

            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            // Count Query
            using (var countCmd = new NpgsqlCommand(
                @"SELECT COUNT(*) FROM tbl_branches;", con))
            {
                result.TotalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            using var cmd = new NpgsqlCommand(@"
            SELECT 
                b.id, 
                b.brand_name, 
                gw.username, 
                gw.pass_text, 
                cdl.secret_key, 
                cdl.sender_id, 
                u.status, 
                m.name
            FROM tbl_branches b
            LEFT JOIN tbl_credentials cdl ON b.credential_id = cdl.id
            LEFT JOIN tbl_merchants m ON b.merchant_id = m.id
            LEFT JOIN tbl_users u ON b.id = u.branch_id
            LEFT JOIN tbl_api_gateway gw ON u.api_gw_id = gw.id
            LIMIT @pageSize OFFSET @offset;;
        ", con);


            cmd.Parameters.AddWithValue("@pageSize", pageSize);
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new KeyListDao
                {
                    branchId = reader.GetGuid(0),


                    brandName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    username = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    password = reader.IsDBNull(3) ? "" : reader.GetString(3),

                    secretKey = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    senderId = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    status = reader.IsDBNull(6) ? "" : reader.GetString(6),

                    merchantName = reader.IsDBNull(7) ? "" : reader.GetString(7)

                });
            }

            result.Items = items;
            result.Page = page;
            result.PageSize = pageSize;

            return result;
        }

        public async Task<PaginatedList<PasswordListDao>> GetAllPasswordList(int page, int pageSize)
        {
            var result = new PaginatedList<PasswordListDao>();
            var items = new List<PasswordListDao>();

            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            // Count Query
            using (var countCmd = new NpgsqlCommand(
                @"SELECT COUNT(*) FROM tbl_message_decrypt;", con))
            {
                result.TotalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            using var cmd = new NpgsqlCommand(@"
            SELECT 
                m.name AS merchantName, 
                b.brand_name AS brandName, 
                CASE WHEN msgd.merchant_wrapped_dek IS NOT NULL THEN 'CONFIGURED' ELSE 'NOT SET' END AS password_status, 
                msgd.password_exp AS passwordExp, 
                msgd.updated_at AS updatedAt
            FROM tbl_users u
            LEFT JOIN tbl_branches b ON u.branch_id = b.id
            LEFT JOIN tbl_merchants m ON b.merchant_id = m.id
            LEFT JOIN tbl_credentials cdl ON b.credential_id = cdl.id
            LEFT JOIN tbl_message_decrypt msgd ON u.msg_decrypt_id = msgd.id
            WHERE u.msg_decrypt_id IS NOT NULL
            LIMIT @pageSize OFFSET @offset
            ;
        ", con);


            cmd.Parameters.AddWithValue("@pageSize", pageSize);
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new PasswordListDao
                {
                    merchantName = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    brandName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    password = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    passwordExp = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),

                    updatedAt = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4)

                });
            }

            result.Items = items;
            result.Page = page;
            result.PageSize = pageSize;

            return result;
        }

        public async Task<string> FindSecretKeyByBranchId(Guid branchId)
        {
            String secretKey = "";
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();
            using var cmd = new NpgsqlCommand(
                 "SELECT cdl.secret_key, cdl.encrypted_secret_key, cdl.key_version " +
                 "FROM tbl_branches b " +
                 "LEFT JOIN tbl_credentials cdl ON b.credential_id = cdl.id " +
                 "WHERE b.id = @branchId",
                 con
             );


            cmd.Parameters.AddWithValue("branchId", branchId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                int keyVersion = reader.IsDBNull(2) ? 1 : reader.GetInt32(2);

                if (keyVersion >= 2 && !reader.IsDBNull(1))
                {
                    secretKey = ServerKeyUtil.UnwrapDek(reader.GetString(1));
                }
                else
                {
                    secretKey = reader.IsDBNull(0) ? "" : reader.GetString(0);
                }
            }
            return secretKey;
        }
        public async Task CreatePassword(AddPasswordDto dto)
        {

            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            using var tx = await con.BeginTransactionAsync();

            try
            {
                using var messageDecryptCmd = new NpgsqlCommand(@"
            INSERT INTO tbl_message_decrypt (
                id, password, password_exp, created_at, updated_at
            )
            VALUES (
                @id, @password, @password_exp, NOW(), NOW()
            )
            RETURNING id;
        ", con, tx);

                var decryptId = Guid.NewGuid();
                messageDecryptCmd.Parameters.AddWithValue("@id", decryptId);
                messageDecryptCmd.Parameters.AddWithValue("@password", dto.password);
                messageDecryptCmd.Parameters.AddWithValue("@password_exp", DateTime.UtcNow.AddMonths(3));

                var result = await messageDecryptCmd.ExecuteScalarAsync();
                if (result is not Guid)
                    throw new Exception("Failed to insert tbl_message_decrypt");

                using var updateUserCmd = new NpgsqlCommand(@"
            UPDATE tbl_users
            SET 
                msg_decrypt_id = @decryptId,
                updated_at = NOW()
            WHERE branch_id = @branch_id
            RETURNING id;
        ", con, tx);

                updateUserCmd.Parameters.AddWithValue("@decryptId", decryptId);
                updateUserCmd.Parameters.AddWithValue("@password_exp", DateTime.UtcNow.AddMonths(3));
                updateUserCmd.Parameters.AddWithValue("@branch_id", dto.branch_id);

                var userObj = await updateUserCmd.ExecuteScalarAsync();
                if (userObj is not Guid userId)
                    throw new Exception("User not found or update failed");

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<List<BrandNames>> GetBranchesForPassphrase(Guid userId)
        {
            var branches = new List<BrandNames>();
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
                SELECT b.id, b.brand_name
                FROM tbl_users u
                JOIN tbl_branches b ON u.branch_id = b.id
                JOIN tbl_credentials cdl ON b.credential_id = cdl.id
                LEFT JOIN tbl_message_decrypt msgd ON u.msg_decrypt_id = msgd.id
                WHERE u.merchant_id = (
                    SELECT merchant_id FROM tbl_users WHERE id = @userId
                )
                AND u.api_gw_id IS NOT NULL
                AND (msgd.merchant_wrapped_dek IS NULL)", con);

            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                branches.Add(new BrandNames
                {
                    id = reader.GetGuid(0),
                    brand_name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    username = "",
                    password = ""
                });
            }
            return branches;
        }

        public async Task SetupPassphrase(Guid userId, SetupPassphraseDto dto)
        {
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            // Verify the branch belongs to this merchant
            using (var verifyCmd = new NpgsqlCommand(@"
                SELECT COUNT(*) FROM tbl_users
                WHERE branch_id = @branch_id
                AND merchant_id = (SELECT merchant_id FROM tbl_users WHERE id = @userId)", con))
            {
                verifyCmd.Parameters.AddWithValue("@branch_id", dto.branch_id);
                verifyCmd.Parameters.AddWithValue("@userId", userId);
                int count = Convert.ToInt32(await verifyCmd.ExecuteScalarAsync());
                if (count == 0)
                    throw new UnauthorizedAccessException("Branch does not belong to your merchant");
            }

            string secretKey = await FindSecretKeyByBranchId(dto.branch_id);
            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("No secret key found for this branch");

            string wrappedDek = ServerKeyUtil.WrapDekWithPassphrase(
                secretKey, dto.passphrase, out string saltHex, out string ivHex);

            string passphraseHash = BCrypt.Net.BCrypt.HashPassword(dto.passphrase);

            using var tx = await con.BeginTransactionAsync();

            try
            {
                Guid? existingDecryptId = null;
                using (var checkCmd = new NpgsqlCommand(@"
                    SELECT u.msg_decrypt_id FROM tbl_users u
                    WHERE u.branch_id = @branch_id AND u.msg_decrypt_id IS NOT NULL", con, tx))
                {
                    checkCmd.Parameters.AddWithValue("@branch_id", dto.branch_id);
                    var obj = await checkCmd.ExecuteScalarAsync();
                    if (obj != null && obj != DBNull.Value)
                        existingDecryptId = (Guid)obj;
                }

                if (existingDecryptId.HasValue)
                {
                    using var updateCmd = new NpgsqlCommand(@"
                        UPDATE tbl_message_decrypt SET
                            merchant_wrapped_dek = @wrapped_dek,
                            dek_salt = COALESCE(dek_salt, @salt),
                            dek_iv = COALESCE(dek_iv, @iv),
                            passphrase_hash = @hash,
                            password_exp = @exp,
                            updated_at = NOW()
                        WHERE id = @id", con, tx);

                    updateCmd.Parameters.AddWithValue("@wrapped_dek", wrappedDek);
                    updateCmd.Parameters.AddWithValue("@salt", saltHex);
                    updateCmd.Parameters.AddWithValue("@iv", ivHex);
                    updateCmd.Parameters.AddWithValue("@hash", passphraseHash);
                    updateCmd.Parameters.AddWithValue("@exp", DateTime.UtcNow.AddMonths(3));
                    updateCmd.Parameters.AddWithValue("@id", existingDecryptId.Value);
                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    var decryptId = Guid.NewGuid();
                    using var insertCmd = new NpgsqlCommand(@"
                        INSERT INTO tbl_message_decrypt (
                            id, password, merchant_wrapped_dek, dek_salt, dek_iv,
                            passphrase_hash, password_exp, created_at, updated_at
                        ) VALUES (
                            @id, '', @wrapped_dek, @salt, @iv,
                            @hash, @exp, NOW(), NOW()
                        )", con, tx);

                    insertCmd.Parameters.AddWithValue("@id", decryptId);
                    insertCmd.Parameters.AddWithValue("@wrapped_dek", wrappedDek);
                    insertCmd.Parameters.AddWithValue("@salt", saltHex);
                    insertCmd.Parameters.AddWithValue("@iv", ivHex);
                    insertCmd.Parameters.AddWithValue("@hash", passphraseHash);
                    insertCmd.Parameters.AddWithValue("@exp", DateTime.UtcNow.AddMonths(3));
                    await insertCmd.ExecuteNonQueryAsync();

                    using var linkCmd = new NpgsqlCommand(@"
                        UPDATE tbl_users SET msg_decrypt_id = @did, updated_at = NOW()
                        WHERE branch_id = @bid RETURNING id", con, tx);
                    linkCmd.Parameters.AddWithValue("@did", decryptId);
                    linkCmd.Parameters.AddWithValue("@bid", dto.branch_id);

                    var userObj = await linkCmd.ExecuteScalarAsync();
                    if (userObj is not Guid)
                        throw new Exception("User not found or update failed");
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }

    }

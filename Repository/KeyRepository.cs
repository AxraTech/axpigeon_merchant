using AxpigeonApp.Dao;
using AxpigeonApp.Dto;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
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
                msgd.password, 
                msgd.password_exp AS passwordExp, 
                msgd.updated_at AS updatedAt,
                cdl.secret_key AS secretKey
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
                    password = reader.IsDBNull(2) ? "" : DecryptMessage(reader.GetString(5),reader.GetString(2)),
                    passwordExp = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),

                    updatedAt = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4)

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
            Console.WriteLine(secretKey);
            Console.WriteLine(base64Message);

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



        public async Task<string> FindSecretKeyByBranchId(Guid branchId)
        {
            String secretKey = "";
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();
            using var cmd = new NpgsqlCommand(
                 "SELECT cdl.secret_key AS secretKey " +   // ✅ space added
                 "FROM tbl_branches b " +
                 "LEFT JOIN tbl_credentials cdl ON b.credential_id = cdl.id " +
                 "WHERE b.id = @branchId",
                 con
             );


            cmd.Parameters.AddWithValue("branchId", branchId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                secretKey  = reader.GetString(0);
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
                // 1. Insert into tbl_message_decrypt
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

                // 2. UPDATE tbl_users
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
    }

    }

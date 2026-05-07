using AxpigeonApp.Dao;
using AxpigeonApp.Dto;
using Npgsql;
namespace AxpigeonApp.Repository
{
    public class MerchantRepository : IMerchantRepository
    {
        private readonly IConfiguration _config;
        private readonly string _conn;  

        public MerchantRepository(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _conn = _config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<UserDao?> GetUserById(Guid id)
        {
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
        SELECT id, email, password, role, password_exp,status
        FROM tbl_users
        WHERE id = @id", con);

            cmd.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Uuid).Value = id;

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            var user = new UserDao
            {
                id = reader.GetGuid(reader.GetOrdinal("id")),
                email = reader.IsDBNull(reader.GetOrdinal("email"))
                    ? ""
                    : reader.GetString(reader.GetOrdinal("email")),
                password = reader.IsDBNull(reader.GetOrdinal("password"))
                    ? ""
                    : reader.GetString(reader.GetOrdinal("password")),
                role = reader.IsDBNull(reader.GetOrdinal("role"))
                    ? ""
                    : reader.GetString(reader.GetOrdinal("role")),
                password_exp = reader.IsDBNull(reader.GetOrdinal("password_exp"))
                    ? ""
                    : reader.GetString(reader.GetOrdinal("password_exp")),
                status = reader.IsDBNull(reader.GetOrdinal("status"))
                    ? ""
                    : reader.GetString(reader.GetOrdinal("status"))
            };

            return user;
        }

        public async Task UpdatePassword(Guid userId, string hashedPassword)
        {
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();
            using var cmd = new NpgsqlCommand(
             "UPDATE tbl_users SET password = @password, password_exp = @password_exp, updated_at = NOW() WHERE id = @userId",
             con
         );
            cmd.Parameters.AddWithValue("password", hashedPassword);
            cmd.Parameters.AddWithValue("password_exp", DateTime.UtcNow.AddMonths(3));
            cmd.Parameters.AddWithValue("userId", userId);
            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            if (rowsAffected == 0)
            {
                throw new KeyNotFoundException("User not found for password update.");
            }

        }

        //Get All Merchants By Admin
        public async Task<PaginatedList<BranchDao>> GetAllMerchantsByMerchant(Guid userId, int page,int pageSize)
        {
            var result = new PaginatedList<BranchDao>();
            var items = new List<BranchDao>();
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

            // 2️⃣ Count branches of THIS merchant
            using (var countCmd = new NpgsqlCommand(
                @"SELECT COUNT(*) 
          FROM tbl_users 
          WHERE role = 'BRANCH' AND merchant_id = @merchant_id;", con))
            {
                countCmd.Parameters.AddWithValue("@merchant_id", merchantId);
                result.TotalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            using var cmd = new NpgsqlCommand(@"
            SELECT 
            b.id,
            b.brand_name,
            snf.mpt,
            snf.mytel,
            snf.u9,
            snf.atom,
            snf.mec,
            snf.purchase_atom,
            snf.purchase_mpt,
            snf.purchase_u9,
            snf.purchase_mytel,
            snf.purchase_mec,
            b.created_at,
            b.updated_at,
            u.email,
            u.status,
            p.name AS provider_name,
            m.name AS merchant_name
        FROM tbl_branches b
        LEFT JOIN tbl_sms_info snf ON b.sms_info_id = snf.id
        LEFT JOIN tbl_users u ON b.id = u.branch_id
        LEFT JOIN tbl_provider p ON b.provider_id = p.id
        LEFT JOIN tbl_merchants m ON b.merchant_id = m.id
        WHERE role = 'BRANCH' AND u.merchant_id = @merchant_id
            ORDER BY m.created_at DESC
            LIMIT @pageSize OFFSET @offset;;
        ", con);


            cmd.Parameters.AddWithValue("@merchant_id", merchantId);
            cmd.Parameters.AddWithValue("@pageSize", pageSize);
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new BranchDao
                {
                    id = reader.GetGuid(reader.GetOrdinal("id")),
                    brand_name = reader.GetString(reader.GetOrdinal("brand_name")),
                    mpt = reader.IsDBNull(reader.GetOrdinal("mpt")) ? "" : reader.GetString(reader.GetOrdinal("mpt")),
                    mytel = reader.IsDBNull(reader.GetOrdinal("mytel")) ? "" : reader.GetString(reader.GetOrdinal("mytel")),
                    u9 = reader.IsDBNull(reader.GetOrdinal("u9")) ? "" : reader.GetString(reader.GetOrdinal("u9")),
                    atom = reader.IsDBNull(reader.GetOrdinal("atom")) ? "" : reader.GetString(reader.GetOrdinal("atom")),
                    mec = reader.IsDBNull(reader.GetOrdinal("mec")) ? "" : reader.GetString(reader.GetOrdinal("mec")),
                    purchase_atom = reader.IsDBNull(reader.GetOrdinal("purchase_atom")) ? "" : reader.GetString(reader.GetOrdinal("purchase_atom")),
                    purchase_mpt = reader.IsDBNull(reader.GetOrdinal("purchase_mpt")) ? "" : reader.GetString(reader.GetOrdinal("purchase_mpt")),
                    purchase_u9 = reader.IsDBNull(reader.GetOrdinal("purchase_u9")) ? "" : reader.GetString(reader.GetOrdinal("purchase_u9")),
                    purchase_mytel = reader.IsDBNull(reader.GetOrdinal("purchase_mytel")) ? "" : reader.GetString(reader.GetOrdinal("purchase_mytel")),
                    purchase_mec = reader.IsDBNull(reader.GetOrdinal("purchase_mec")) ? "" : reader.GetString(reader.GetOrdinal("purchase_mec")),
                    status = reader.GetString(reader.GetOrdinal("status")),
                    provider_name = reader.GetString(reader.GetOrdinal("provider_name")),
                    merchant_name = reader.GetString(reader.GetOrdinal("merchant_name")),
                    email = reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString(reader.GetOrdinal("email")),
                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    updated_at = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            result.Items = items;
            result.Page = page;
            result.PageSize = pageSize;

            return result;
        }



        public async Task<MerchantDetailDao> GetAllBranchesByMerchant(
    Guid id, int page, int pageSize)
        {
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            // Always create the object
            var user = new MerchantDetailDao();

            // ============================
            // 1. Count branches
            // ============================
            const string countSql = @"
        SELECT COUNT(*) 
        FROM tbl_branches 
        WHERE merchant_id = @merchant_id";

            using var countCmd = new NpgsqlCommand(countSql, con);
            countCmd.Parameters.AddWithValue("@merchant_id", id);

            int totalBranches = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            // ============================
            // 2. Get paginated branches
            // ============================
            const string branchSql = @"
        SELECT 
            b.id,
            b.brand_name,
            snf.mpt,
            snf.mytel,
            snf.u9,
            snf.atom,
            snf.mec,
            snf.purchase_atom,
            snf.purchase_mpt,
            snf.purchase_u9,
            snf.purchase_mytel,
            snf.purchase_mec,
            b.created_at,
            b.updated_at,
            u.email,
            u.status,
            p.name AS provider_name,
            m.name AS merchant_name
        FROM tbl_branches b
        LEFT JOIN tbl_sms_info snf ON b.sms_info_id = snf.id
        LEFT JOIN tbl_users u ON b.id = u.branch_id
        LEFT JOIN tbl_provider p ON b.provider_id = p.id
        LEFT JOIN tbl_merchants m ON b.merchant_id = m.id
        WHERE b.merchant_id = @merchant_id
        ORDER BY b.created_at DESC
        OFFSET @offset LIMIT @limit;";

            using var branchCmd = new NpgsqlCommand(branchSql, con);
            branchCmd.Parameters.AddWithValue("@merchant_id", id);
            branchCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
            branchCmd.Parameters.AddWithValue("@limit", pageSize);

            var branches = new List<BranchDao>();

            using (var reader = await branchCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    branches.Add(new BranchDao
                    {
                        id = reader.GetGuid(reader.GetOrdinal("id")),
                        brand_name = reader.GetString(reader.GetOrdinal("brand_name")),
                        mpt = reader.IsDBNull(reader.GetOrdinal("mpt")) ? "" : reader.GetString(reader.GetOrdinal("mpt")),
                        mytel = reader.IsDBNull(reader.GetOrdinal("mytel")) ? "" : reader.GetString(reader.GetOrdinal("mytel")),
                        u9 = reader.IsDBNull(reader.GetOrdinal("u9")) ? "" : reader.GetString(reader.GetOrdinal("u9")),
                        atom = reader.IsDBNull(reader.GetOrdinal("atom")) ? "" : reader.GetString(reader.GetOrdinal("atom")),
                        mec = reader.IsDBNull(reader.GetOrdinal("mec")) ? "" : reader.GetString(reader.GetOrdinal("mec")),
                        purchase_atom = reader.IsDBNull(reader.GetOrdinal("purchase_atom")) ? "" : reader.GetString(reader.GetOrdinal("purchase_atom")),
                        purchase_mpt = reader.IsDBNull(reader.GetOrdinal("purchase_mpt")) ? "" : reader.GetString(reader.GetOrdinal("purchase_mpt")),
                        purchase_u9 = reader.IsDBNull(reader.GetOrdinal("purchase_u9")) ? "" : reader.GetString(reader.GetOrdinal("purchase_u9")),
                        purchase_mytel = reader.IsDBNull(reader.GetOrdinal("purchase_mytel")) ? "" : reader.GetString(reader.GetOrdinal("purchase_mytel")),
                        purchase_mec = reader.IsDBNull(reader.GetOrdinal("purchase_mec")) ? "" : reader.GetString(reader.GetOrdinal("purchase_mec")),
                        status = reader.GetString(reader.GetOrdinal("status")),
                        provider_name = reader.GetString(reader.GetOrdinal("provider_name")),
                        merchant_name = reader.GetString(reader.GetOrdinal("merchant_name")),
                        email = reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString(reader.GetOrdinal("email")),
                        created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                        updated_at = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                    });
                }
            }

            // ============================
            // 3. Attach pagination (SAFE)
            // ============================
            user.branches = new PaginatedList<BranchDao>
            {
                Items = branches,     // [] if no data
                Page = page,
                PageSize = pageSize,
                TotalCount = totalBranches
            };

            return user;
        }


        public async Task<List<ProviderDao>> GetAllProviders()
        {
            var items = new List<ProviderDao>();

            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();


            using var cmd = new NpgsqlCommand(@"
            SELECT 
                p.id, 
                p.atom, 
                p.mpt, 
                p.mytel, 
                p.name, 
                p.u9, 
                p.mec, 
                p.total_purchase
            FROM tbl_provider p;
        ", con);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new ProviderDao
                {
                    id = reader.GetGuid(0),

                    atom = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    mpt = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    mytel = reader.IsDBNull(3) ? "" : reader.GetString(3),

                    name = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    u9 = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    mec = reader.IsDBNull(6) ? "" : reader.GetString(6)
                });
            }


            return items;
        }


        public async Task<MerchantDao> Create(MerchantCreateDto dto)
        {
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            using var tx = await con.BeginTransactionAsync(); // Start transaction

            try
            {
              
                // 2. Insert Merchant
                using var merchantCmd = new NpgsqlCommand(@"
            INSERT INTO tbl_merchants (
               id, name, phone, address, created_at, updated_at
            )
            VALUES (
                @id,@name, @phone, @address, NOW(), NOW()
            )
            RETURNING id;", con, tx);

                merchantCmd.Parameters.AddWithValue("@id", Guid.NewGuid());
                merchantCmd.Parameters.AddWithValue("@name", dto.name);
                merchantCmd.Parameters.AddWithValue("@phone", dto.phone);
                merchantCmd.Parameters.AddWithValue("@address", dto.address);

                var merchantObj = await merchantCmd.ExecuteScalarAsync();
                if (merchantObj is not Guid merchantId)
                    throw new InvalidOperationException("Failed to insert merchant or returned id is null.");

                // 3. Insert User
                using var userCmd = new NpgsqlCommand(@"
            INSERT INTO tbl_users (
               id, email, password, password_exp,
                role, merchant_id,status, created_at, updated_at
            )
            VALUES (
               @id, @email, @password, @password_exp,
                @role, @merchant_id,@status, NOW(), NOW()
            )
            RETURNING id;", con, tx);
                userCmd.Parameters.AddWithValue("@id", Guid.NewGuid());
                userCmd.Parameters.AddWithValue("@email", dto.email);
                userCmd.Parameters.AddWithValue("@password", dto.password);
                userCmd.Parameters.AddWithValue("@password_exp", DateTime.UtcNow.AddMonths(3));
                userCmd.Parameters.AddWithValue("@role", "MERCHANT");
                userCmd.Parameters.AddWithValue("@merchant_id", merchantId);
                userCmd.Parameters.AddWithValue("@status", "SUCCESS");

                var userObj = await userCmd.ExecuteScalarAsync();
                if (userObj is not Guid userId)
                    throw new InvalidOperationException("Failed to insert user or returned id is null.");

                // Commit transaction after all 3 succeed
                await tx.CommitAsync();

                return new MerchantDao
                {
                    id = userId,
                    email = dto.email
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<MerchantDao> CreateSubMerch(SubMerchantCreateDto dto)
        {
            await using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();
            await using var tx = await con.BeginTransactionAsync();

            try
            {
                // 1️⃣ Insert SMS Info
                var smsInfoId = Guid.NewGuid();

                await using var smsInfoCmd = new NpgsqlCommand(@"
            INSERT INTO tbl_sms_info (
                id, atom, mpt, mytel, u9, mec,
                purchase_atom, purchase_mpt, purchase_mytel, purchase_u9, purchase_mec
            )
            VALUES (
                @id, @atom, @mpt, @mytel, @u9, @mec,
                @purchase_atom, @purchase_mpt, @purchase_mytel, @purchase_u9, @purchase_mec
            );", con, tx);

                smsInfoCmd.Parameters.AddWithValue("@id", smsInfoId);
                smsInfoCmd.Parameters.AddWithValue("@atom", dto.atom);
                smsInfoCmd.Parameters.AddWithValue("@mpt", dto.mpt);
                smsInfoCmd.Parameters.AddWithValue("@mytel", dto.mytel);
                smsInfoCmd.Parameters.AddWithValue("@u9", dto.u9);
                smsInfoCmd.Parameters.AddWithValue("@mec", dto.mec);
                smsInfoCmd.Parameters.AddWithValue("@purchase_atom", dto.atom);
                smsInfoCmd.Parameters.AddWithValue("@purchase_mpt", dto.mpt);
                smsInfoCmd.Parameters.AddWithValue("@purchase_mytel", dto.mytel);
                smsInfoCmd.Parameters.AddWithValue("@purchase_u9", dto.u9);
                smsInfoCmd.Parameters.AddWithValue("@purchase_mec", dto.mec);

                await smsInfoCmd.ExecuteNonQueryAsync();

                // 2️⃣ Insert Branch (Sub Merchant)
                var branchId = Guid.NewGuid();

                await using var merchantCmd = new NpgsqlCommand(@"
            INSERT INTO tbl_branches (
                id, brand_name, address, alert_amount, sms_info_id,merchant_id,provider_id,
                created_at, updated_at
            )
            VALUES (
                @id, @brand_name, @address, @alert_amount, @sms_info_id,@merchant_id,@provider_id,
                NOW(), NOW()
            );", con, tx);

                merchantCmd.Parameters.AddWithValue("@id", branchId);
                merchantCmd.Parameters.AddWithValue("@brand_name", dto.brandName);
                merchantCmd.Parameters.AddWithValue("@address", dto.address);
                merchantCmd.Parameters.AddWithValue("@alert_amount", dto.alertSms);
                merchantCmd.Parameters.AddWithValue("@sms_info_id", smsInfoId);
                merchantCmd.Parameters.AddWithValue("@merchant_id", dto.merchantId);
                merchantCmd.Parameters.AddWithValue("@provider_id", dto.providerId);
                await merchantCmd.ExecuteNonQueryAsync();

                // 3️⃣ Insert User
                var userId = Guid.NewGuid();

                await using var userCmd = new NpgsqlCommand(@"
            INSERT INTO tbl_users (
                id, email, password, password_exp,
                role, merchant_id,branch_id, status,
                created_at, updated_at
            )
            VALUES (
                @id, @email, @password, @password_exp,
                @role, @merchant_id,@branch_id, @status,
                NOW(), NOW()
            );", con, tx);

                userCmd.Parameters.AddWithValue("@id", userId);
                userCmd.Parameters.AddWithValue("@email", dto.email);
                userCmd.Parameters.AddWithValue("@password", dto.password);
                userCmd.Parameters.AddWithValue("@password_exp", DateTime.UtcNow.AddMonths(3));
                userCmd.Parameters.AddWithValue("@role", "BRANCH");
                userCmd.Parameters.AddWithValue("@merchant_id", dto.merchantId); 
                userCmd.Parameters.AddWithValue("@branch_id", branchId);
                userCmd.Parameters.AddWithValue("@status", "SUCCESS");

                await userCmd.ExecuteNonQueryAsync();

                await tx.CommitAsync();

                return new MerchantDao
                {
                    id = userId,
                    email = dto.email
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }


        public async Task<string> KeyGenerate(
     Guid id,
     string secretKey,
     string senderId,
     string username,
     string hashedPassword,
     string password)
        {
            await using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();
            await using var tx = await con.BeginTransactionAsync();

            try
            {
                // 1️⃣ Insert Credentials
                var credentialId = Guid.NewGuid();

                await using var cdlCmd = new NpgsqlCommand(@"
            INSERT INTO tbl_credentials (id, secret_key, sender_id)
            VALUES (@id, @secret_key, @sender_id);", con, tx);

                cdlCmd.Parameters.AddWithValue("@id", credentialId);
                cdlCmd.Parameters.AddWithValue("@secret_key", secretKey);
                cdlCmd.Parameters.AddWithValue("@sender_id", senderId);

                await cdlCmd.ExecuteNonQueryAsync();

                // 2️⃣ Insert API Gateway
                var gatewayId = Guid.NewGuid();

                await using var gatewayCmd = new NpgsqlCommand(@"
            INSERT INTO tbl_api_gateway (id, username, password,pass_text)
            VALUES (@id, @username, @password,@pass_text);", con, tx);

                gatewayCmd.Parameters.AddWithValue("@id", gatewayId);
                gatewayCmd.Parameters.AddWithValue("@username", username);
                gatewayCmd.Parameters.AddWithValue("@password", hashedPassword);
                gatewayCmd.Parameters.AddWithValue("@pass_text", password);

                await gatewayCmd.ExecuteNonQueryAsync();

                // 3️⃣ Update Branch
                await using var branchCmd = new NpgsqlCommand(@"
            UPDATE tbl_branches
            SET credential_id = @credential_id,
                updated_at = NOW()
            WHERE id = @branch_id;", con, tx);

                branchCmd.Parameters.AddWithValue("@credential_id", credentialId);
                branchCmd.Parameters.AddWithValue("@branch_id", id);

                await branchCmd.ExecuteNonQueryAsync();

                // 4 Update User
                await using var userCmd = new NpgsqlCommand(@"
            UPDATE tbl_users
            SET api_gw_id = @api_gateway_id,
                updated_at = NOW()
            WHERE branch_id = @branch_id;", con, tx);

                userCmd.Parameters.AddWithValue("@api_gateway_id", gatewayId);
                userCmd.Parameters.AddWithValue("@branch_id", id);

                await userCmd.ExecuteNonQueryAsync();
                await tx.CommitAsync(); // ✅ REQUIRED
                return "SUCCESS";
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }


    }
}

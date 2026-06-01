using AxpigeonApp.Dao;
using AxpigeonApp.Dto;
using AxpigeonApp.Repository;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace AxpigeonApp.Services

{
    public  class MerchantService : IMerchantService
    {
        private readonly IMerchantRepository _repo;
        private readonly ILogger<MerchantService> _logger;
        private const string CharSet =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private const int KeyLength = 32;

        public MerchantService(IMerchantRepository repo, ILogger<MerchantService> logger)
        {
            _repo = repo;
            _logger = logger;
        }
        public static string GenerateSecretKey()
        {
            var bytes = new byte[KeyLength];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            var result = new StringBuilder(KeyLength);
            foreach (var b in bytes)
            {
                result.Append(CharSet[b % CharSet.Length]);
            }
            return result.ToString();
        }
       

        public async Task ChangePassword(Guid userId, string old_password, string new_password, string confirm_password)
        {
            if (new_password != confirm_password)
            {
                throw new ArgumentException("New password and confirm password do not match.");
            }
            var user = await _repo.GetUserById(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }
            if (!BCrypt.Net.BCrypt.Verify(old_password, user.password))
            {
                throw new UnauthorizedAccessException("Old password is incorrect.");
            }
            var hashedNewPassword = BCrypt.Net.BCrypt.HashPassword(new_password);
            await _repo.UpdatePassword(userId, hashedNewPassword);
        }
        
        public async Task<PaginatedList<BranchDao>> GetAllMerchantsByMerchant(Guid userId,int page,int pageSize)
        {
            var dao = await _repo.GetAllMerchantsByMerchant(userId,page, pageSize);

            var mappedItems = dao.Items.Select(x => new BranchDao
            {
                id = x.id,
                brand_name = x.brand_name,
                mpt = x.mpt,
                mytel = x.mytel,
                u9 = x.u9,
                atom = x.atom,
                mec = x.mec,

                purchase_atom = x.purchase_atom,
                purchase_mpt = x.purchase_mpt,
                purchase_u9 = x.purchase_u9,
                purchase_mytel = x.purchase_mytel,
                purchase_mec = x.purchase_mec,

                status = x.status,
                provider_name = x.provider_name,
                merchant_name = x.merchant_name,

                email = x.email,
                created_at = x.created_at,
                updated_at = x.updated_at
            }).ToList();


            // Return as PaginatedList<UserDao>
            return new PaginatedList<BranchDao>
            {
                Items = mappedItems,
                TotalCount = dao.TotalCount,
                Page = dao.Page,
                PageSize = dao.PageSize
            };
        }


        public async Task<MerchantDetailDao?> GetAllBranchesByMerchant(Guid id, int page, int pageSize)
        {
            var user = await _repo.GetAllBranchesByMerchant(id, page, pageSize);
            if (user == null)
                return null;
            return new MerchantDetailDao
            {
                
                branches = user.branches
            };
        }

        public async Task<List<ProviderDao>> GetAllProviders()
        {
            var providers = await _repo.GetAllProviders();

            var mappedItems = providers.Select(x => new ProviderDao
            {
                id = x.id,
                mpt = x.mpt,
                u9 = x.u9,
                atom = x.atom,
                mytel = x.mytel,
                mec = x.mec,
                name = x.name,
                total_purchase = x.atom,
               
            }).ToList();

            // Return as PaginatedList<UserDao>
            return mappedItems;
        }

        public async Task<MerchantDao> Create(MerchantCreateDto dto)
        {
            dto.password = BCrypt.Net.BCrypt.HashPassword(dto.password);

            var createdMerchant = await _repo.Create(dto);
            return createdMerchant;
        }
        public async Task<MerchantDao> CreateSubMerch(SubMerchantCreateDto dto)
        {
            dto.password = BCrypt.Net.BCrypt.HashPassword(dto.password);

            var createdSubMerchant = await _repo.CreateSubMerch(dto);
            return createdSubMerchant;
        }

        public async Task<String> KeyGenerate(Guid id,
    string username,
    string password)
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            var secretKey = GenerateSecretKey();
            var senderId = RandomNumberGenerator
         .GetInt32(100_000_000, 1_000_000_000)
         .ToString();
            _logger.LogDebug(
        "Generated credentials. MerchantId={MerchantId}, SenderId={SenderId}, SecretKeyLength={KeyLength}",
        id,
        senderId,
        secretKey.Length
    );

            return await _repo.KeyGenerate(id, secretKey,
         senderId,
         username,
         hashedPassword, password);
                
            }
    }
}

using AxpigeonApp.Dao;
using AxpigeonApp.Dto;
using AxpigeonApp.Repository;
using System.Security.Cryptography;
using System.Text;

namespace AxpigeonApp.Services
{
    public class KeyService : IKeyService
    {
        private readonly IKeyRepository _repo;
        public KeyService(IKeyRepository repo)
        {
            _repo = repo;
        }
        public async Task<PaginatedList<KeyListDao>> GetAllKeys(int page, int pageSize)
        {
            var dao = await _repo.GetAllKeys(page, pageSize);

            var mappedItems = dao.Items.Select(x => new KeyListDao
            {
                 branchId= x.branchId,
                username = x.username,
                password = x.password,
                secretKey = x.secretKey,
                senderId  = x.senderId,
                status = x.status,
                brandName = x.brandName,
                merchantName = x.merchantName
            }).ToList();

            // Return as PaginatedList<KeyListDao>
            return new PaginatedList<KeyListDao>
            {
                Items = mappedItems,
                TotalCount = dao.TotalCount,
                Page = dao.Page,
                PageSize = dao.PageSize
            };
        }

        public async Task<PaginatedList<PasswordListDao>>  GetAllPasswordList(int page, int pageSize)
        {
            var dao = await _repo.GetAllPasswordList(page, pageSize);

            var mappedItems = dao.Items.Select(x => new PasswordListDao
            {
                merchantName = x.merchantName,
                brandName = x.brandName,
                password = x.password,
                passwordExp = x.passwordExp,
                updatedAt = x.updatedAt
            }).ToList();

            // Return as PaginatedList<PasswordListDao>
            return new PaginatedList<PasswordListDao>
            {
                Items = mappedItems,
                TotalCount = dao.TotalCount,
                Page = dao.Page,
                PageSize = dao.PageSize
            };
        }
        public async Task<List<BrandNames>> GetAllBrandNames()
        {
            var dao = await _repo.GetAllBrandNames();
            var mappedItems = dao.Select(x => new BrandNames
            {
                id = x.id,
                brand_name = x.brand_name,
                username = x.username,
                password = x.password,

            }).ToList();
            return mappedItems;
        }


        public async Task AddPass(AddPasswordDto dto)
        {
            string secretKey = await _repo.FindSecretKeyByBranchId(dto.branch_id);
            dto.password = EncryptMessage(dto.password, secretKey);
            await _repo.CreatePassword(dto);
        }
        public static string EncryptMessage(string content, string secretKey)
        {
            byte[] keyBytes = FormatKey(secretKey);
            byte[] ivBytes = FormatIV(secretKey);

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7; // PKCS5Padding == PKCS7 in .NET
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(content);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

          
            return Convert.ToBase64String(encryptedBytes);
        }
                
        
        private static byte[] FormatIV(string secretKey)
        {
            byte[] ivBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] formattedIV = new byte[16]; // AES block size (16 bytes)

            int len = Math.Min(ivBytes.Length, 16);
            Array.Copy(ivBytes, formattedIV, len);

            return formattedIV;
        }


        private static byte[] FormatKey(string secretKey)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] formattedKey = new byte[32]; // AES-256 (32 bytes)

            int len = Math.Min(keyBytes.Length, 32);
            Array.Copy(keyBytes, formattedKey, len);

            return formattedKey;
        }



    }
}

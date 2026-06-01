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
                updatedAt = x.updatedAt,
                hasPassphrase = x.hasPassphrase,
                status = x.status
            }).ToList();

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
            await _repo.CreatePassword(dto);
        }

        public async Task<List<BrandNames>> GetBranchesForPassphrase(Guid userId)
        {
            return await _repo.GetBranchesForPassphrase(userId);
        }

        public async Task SetupPassphrase(Guid userId, SetupPassphraseDto dto)
        {
            await _repo.SetupPassphrase(userId, dto);
        }
    }
}

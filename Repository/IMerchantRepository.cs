using AxpigeonApp.Dao;
using AxpigeonApp.Dto;
namespace AxpigeonApp.Repository
{
    public interface IMerchantRepository
    {
        Task<PaginatedList<BranchDao>> GetAllMerchantsByMerchant(Guid userId,int page,int pageSize);
        Task<MerchantDetailDao?> GetAllBranchesByMerchant(Guid id,int page,int pageSize);
        Task<MerchantDao> Create(MerchantCreateDto dto);

        Task<MerchantDao> CreateSubMerch(SubMerchantCreateDto dto);

        Task<List<ProviderDao>> GetAllProviders();

        Task<UserDao?> GetUserById(Guid id);

        Task UpdatePassword(Guid userId, string hashedPassword);
        Task<String> KeyGenerate(Guid id, string secretKey,
     string senderId,
     string username,
     string hashedPassword, string password);
    }
}

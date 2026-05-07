using AxpigeonApp.Dao;
using AxpigeonApp.Dto;
namespace AxpigeonApp.Services
{
    public interface IMerchantService
    {
        Task<PaginatedList<BranchDao>> GetAllMerchantsByMerchant(Guid userId,int page,int pageSize);
        Task<MerchantDao> Create(MerchantCreateDto dto);
        Task<MerchantDao> CreateSubMerch(SubMerchantCreateDto dto);

        Task<MerchantDetailDao?> GetAllBranchesByMerchant(Guid id, int page, int pageSize);

        Task<List<ProviderDao>> GetAllProviders();

        Task ChangePassword(Guid userId,string old_password,string new_password,string confirm_password);

        Task<String> KeyGenerate(Guid id,
    string username,
    string password);
    }
}

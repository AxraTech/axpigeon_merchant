using AxpigeonApp.Dao;
using AxpigeonApp.Dto;


namespace AxpigeonApp.Repository
{
    public interface IKeyRepository
    {
        Task<PaginatedList<KeyListDao>> GetAllKeys(int page, int pageSize);

        Task<PaginatedList<PasswordListDao>> GetAllPasswordList(int page, int pageSize);
        Task<List<BrandNames>> GetAllBrandNames();

        Task<string> FindSecretKeyByBranchId(Guid branchId);
        Task CreatePassword(AddPasswordDto dto);
    }
}

using AxpigeonApp.Dao;
using AxpigeonApp.Dto;

namespace AxpigeonApp.Services
{
    public interface IKeyService
    {
        Task<PaginatedList<KeyListDao>> GetAllKeys(int page, int pageSize);

        Task<PaginatedList<PasswordListDao>> GetAllPasswordList(int page, int pageSize);
        Task<List<BrandNames>> GetAllBrandNames();

        Task AddPass(AddPasswordDto dto);
    }
}

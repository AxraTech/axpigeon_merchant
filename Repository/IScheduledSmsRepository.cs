using AxpigeonApp.Dao;
using AxpigeonApp.Dto;

namespace AxpigeonApp.Repository
{
    public interface IScheduledSmsRepository
    {
        Task<List<BrandNames>> GetAllBrandNames(Guid userId);
        Task<ValidateMerchantDao?> FindValidMerchantAsync(Guid branchId);
    }
}

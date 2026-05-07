using AxpigeonApp.Dao;
using AxpigeonApp.Dto;

namespace AxpigeonApp.Repository
{
    public interface ITransactionsRepository
    {
        Task<PaginatedList<TransactionListDao>> GetAllTransactions(Guid userId,int page, int pageSize);
        Task<List<BrandNames>> GetAllBrandNames(Guid userId);

        

        Task<ValidateMerchantDao> FindValidMerchantAsync(SendMessageDto dto);

        
    }
}

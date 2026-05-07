using AxpigeonApp.Dao;
using AxpigeonApp.Dto;
namespace AxpigeonApp.Services
{
    public interface ITransactionsService
    {
        Task<PaginatedList<TransactionListDao>> GetAllTransactions(Guid userId, int page, int pageSize);
        Task<List<BrandNames>> GetAllBrandNames(Guid userId);

        Task SendBulkAsync(SendMessageDto dto);
        Task sendMessage(SendMessageDto dto);
    }
}

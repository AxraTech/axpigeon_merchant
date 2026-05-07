using AxpigeonApp.Dao;
using AxpigeonApp.Repository;
using System.Transactions;

namespace AxpigeonApp.Services
{
    public class HomeService : IHomeService
    {
        private readonly IHomeRepository _repo;
        public HomeService(IHomeRepository repo)
        {
            _repo = repo;
        }
        public async Task<DashboardDao> GetData(Guid userId,int page, int pageSize)
        {
            var dao = await _repo.GetData(userId,page, pageSize);

            var mappedItems = dao.transactions.Items.Select(x => new TransactionListDao
            {
                id = x.id,
                transactionId = x.transactionId,
                brand_name = x.brand_name,
                merchant_name = x.merchant_name,
                message = x.message,
                operator_name = x.operator_name,
                pov_transaction_id = x.pov_transaction_id,
                schedule_date = x.schedule_date,
                sent_at = x.sent_at,
                sms_type = x.sms_type,
                provider_name = x.provider_name,
                pov_campaign_id = x.pov_campaign_id,
                transaction_id = x.transaction_id,
                is_send_now = x.is_send_now,
                transaction_tmp_id = x.transaction_tmp_id,
                status = x.status,
                created_at = x.created_at,
                phone = x.phone,
                msgPassword = x.msgPassword,
            }).ToList();

            return new DashboardDao
            {
                packageList = dao.packageList,
                transactions = new PaginatedList<TransactionListDao>
                {
                    Items = mappedItems,
                    TotalCount = dao.transactions.TotalCount,
                    Page = dao.transactions.Page,
                    PageSize = dao.transactions.PageSize
                }
            };
        }
    }
}

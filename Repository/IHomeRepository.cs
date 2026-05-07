using AxpigeonApp.Dao;

namespace AxpigeonApp.Repository
{
    public interface IHomeRepository
    {
        Task<DashboardDao> GetData(Guid userId, int page, int pageSize);
    }
}

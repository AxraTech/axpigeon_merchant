using AxpigeonApp.Dao;

namespace AxpigeonApp.Services
{
    public interface IHomeService
    {
        Task<DashboardDao> GetData(Guid userId,int page, int pageSize);
    }
}

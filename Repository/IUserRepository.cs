
using AxpigeonApp.Dao;

namespace AxpigeonApp.Repository
{
    public interface IUserRepository 
    {
        Task<UserDao?> GetUserByEmail(string email);
    }
}

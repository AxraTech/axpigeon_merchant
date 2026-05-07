using AxpigeonApp.Dao;

namespace AxpigeonApp.Service
{
    public interface IUserService
    {
        Task<UserDao?> Login(string email, string password);
       
    }
}

using AxpigeonApp.Dao;
using AxpigeonApp.Repository;

namespace AxpigeonApp.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task<UserDao?> Login(string email, string password)
        {
            var user = await _repo.GetUserByEmail(email);

            if (user == null)
                return null;

            // Compare hashed password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.password);

            if (!isPasswordValid)
                return null;


            return new UserDao
            {
                id = user.id,
                email = user.email,
                role = user.role,
                status = user.status,
                merchant_name = user.merchant_name,
                merchant_status = user.merchant_status,
                merchant_status_reason = user.merchant_status_reason
            };
        }
    }
}

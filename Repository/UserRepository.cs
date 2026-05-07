
using AxpigeonApp.Dao;
using Npgsql;

namespace AxpigeonApp.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly IConfiguration _config;
        private readonly string _conn;
        public UserRepository(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _conn = _config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        //Find By Email
        public async Task<UserDao?> GetUserByEmail(string email)
        {
            UserDao? user = null;
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();
            using var cmd = new NpgsqlCommand(
             "SELECT u.id, u.email, u.role, u.password, u.password_exp, u.status, m.name " +
             "FROM tbl_users u " +
             "LEFT JOIN tbl_merchants m ON u.merchant_id = m.id " +
             "WHERE u.email = @email",
             con
         );

            cmd.Parameters.AddWithValue("email", email);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                user = new UserDao
                {
                    id = reader.GetGuid(0),
                    email = reader.GetString(1),
                    role = reader.GetString(2),
                    password = reader.GetString(3),
                    password_exp = reader.GetString(4),
                    status = reader.GetString(5),
                    merchant_name = reader.IsDBNull(6) ? null : reader.GetString(6)
                };
            }
            return user;
        }

    }
    }

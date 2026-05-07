namespace AxpigeonApp.Dao
{
    public class UserDao
    {
        public Guid id { get; set; }

        public string email { get; set; }

        public string password { get; set; }

        public string role { get; set; }

        public string password_exp { get; set; }

        public string status { get; set; }

        public string merchant_name { get; set; }
    }
}

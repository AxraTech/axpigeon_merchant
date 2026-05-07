namespace AxpigeonApp.Dao
{
    public class AuthTokenResponseDao
    {
        public string status { get; set; }
        public string message { get; set; }
        public long expireAt { get; set; }
        public string data { get; set; }
    }
}

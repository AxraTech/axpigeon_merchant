
namespace AxpigeonApp.Dao
{
    public class MerchantDao
    {
        public Guid id { get; set; }
        public string email { get; set; }
        public string role { get; set; } 
        public string name { get; set; }
        public string status { get; set; }

        public string provider_name { get; set; }
        public string merchant_id { get; set; }
        public string phone { get; set; }

        public string address { get; set; }

        public string atom { get; set; }
        public string mytel { get; set; }
        public string u9 { get; set; }
        public string mpt { get; set; }
        public DateTime created_at { get; set; }

        public DateTime updated_at { get; set; }
    }
}

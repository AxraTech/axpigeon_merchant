namespace AxpigeonApp.Dao
{
    public class BranchDao
    {
        public Guid id { get; set; }
        public string email { get; set; }
        public string role { get; set; }
        public string brand_name { get; set; }
        public string merchant_name { get; set; }
        public string provider_name { get; set; }
        public string status { get; set; }


        public string address { get; set; }

        public string atom { get; set; }
        public string mytel { get; set; }
        public string u9 { get; set; }
        public string mpt { get; set; }
        public string mec { get; set; }

        public string purchase_atom { get; set; }
        public string purchase_mytel { get; set; }
        public string purchase_u9 { get; set; }
        public string purchase_mpt { get; set; }
        public string purchase_mec { get; set; }
        public DateTime created_at { get; set; }

        public DateTime updated_at { get; set; }
    }
}

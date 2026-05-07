namespace AxpigeonApp.Dao
{
    public class MerchantDetailDao
    {
        //public Guid id { get; set; }

        //public string merchant_name { get; set; }
        //public string? brand_name { get; set; }
        //public string atom { get; set; }
        //public string mytel { get; set; }
        //public string u9 { get; set; }
        //public string mpt { get; set; }

        public PaginatedList<BranchDao> branches { get; set; }
    }
}

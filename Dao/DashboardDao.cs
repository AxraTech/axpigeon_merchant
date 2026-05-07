namespace AxpigeonApp.Dao
{
    public class DashboardDao
    {
        public List<DashboardSmsPackages> packageList { get; set; } = new();
        
        public PaginatedList<TransactionListDao> transactions { get; set; } = new();
    }
}

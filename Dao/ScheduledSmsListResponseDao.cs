namespace AxpigeonApp.Dao
{
    public class ScheduledSmsListResponseDao
    {
        public string status { get; set; }
        public string message { get; set; }
        public int totalCount { get; set; }
        public List<ScheduledSmsDataDao> items { get; set; }
    }
}

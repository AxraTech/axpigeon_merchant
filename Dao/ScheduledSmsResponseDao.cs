namespace AxpigeonApp.Dao
{
    public class ScheduledSmsResponseDao
    {
        public string status { get; set; }
        public string message { get; set; }
        public ScheduledSmsDataDao data { get; set; }
    }
}

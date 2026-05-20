namespace AxpigeonApp.Dao
{
    public class ScheduledSmsRecipientDao
    {
        public string id { get; set; }
        public string phone { get; set; }
        public string operatorName { get; set; }
        public string status { get; set; }
        public string transactionId { get; set; }
        public string sentAt { get; set; }
        public string errorMessage { get; set; }
    }
}

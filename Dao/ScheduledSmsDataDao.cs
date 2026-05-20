namespace AxpigeonApp.Dao
{
    public class ScheduledSmsDataDao
    {
        public string scheduleId { get; set; }
        public string smsType { get; set; }
        public string brandName { get; set; }
        public string senderId { get; set; }
        public string scheduledAt { get; set; }
        public string scheduleStatus { get; set; }
        public int totalRecipients { get; set; }
        public int pendingCount { get; set; }
        public int sentCount { get; set; }
        public int failedCount { get; set; }
        public int cancelledCount { get; set; }
        public string createdAt { get; set; }
        public string updatedAt { get; set; }
        public List<ScheduledSmsRecipientDao> recipients { get; set; }
    }
}

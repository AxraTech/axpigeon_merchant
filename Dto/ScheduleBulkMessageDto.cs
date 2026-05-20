namespace AxpigeonApp.Dto
{
    public class ScheduleBulkMessageDto
    {
        public string brandName { get; set; }
        public string messageHash { get; set; }
        public List<string> listPhoneNumber { get; set; }
        public string senderId { get; set; }
        public string scheduleDate { get; set; }
        public string token { get; set; }
    }
}

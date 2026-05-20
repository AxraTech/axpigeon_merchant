namespace AxpigeonApp.Dto
{
    public class ScheduleMessageDto
    {
        public Guid id { get; set; }
        public string? phone { get; set; }
        public string message { get; set; }
        public bool isBulk { get; set; }
        public string scheduleDate { get; set; }
        public IFormFile? excelFile { get; set; }
    }
}

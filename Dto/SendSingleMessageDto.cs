namespace AxpigeonApp.Dto
{
    public class SendSingleMessageDto
    {
        public string brand_name { get; set; }

        public string message { get; set; }

        public string phone { get; set; }

        public string sender_id { get; set; }

        public bool is_send_now { get; set; }

        public string? schedule_date { get; set; }

        public string token { get; set; }
    }

}

namespace AxpigeonApp.Dao
{
    public class SendSingleMessageDao
    {
        public Guid id { get; set; }
        public string phone { get; set; }
        public string message { get; set; }

        public bool isBulk { get; set; }

        public string brand_name { get; set; }

         public IFormFile excelFile { get; set; }
    }
}

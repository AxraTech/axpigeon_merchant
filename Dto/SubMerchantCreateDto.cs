namespace AxpigeonApp.Dto
{
    public class SubMerchantCreateDto
    {
        public required string brandName { get; set; }

        public required Guid merchantId { get; set; }

        public required Guid providerId { get; set; }
        public required string email { get; set; }

        public required string alertSms { get; set; }
        public required string mytel { get; set; }
        public required string atom { get; set; }
        public required string u9 { get; set; }
        public required string mpt { get; set; }
        public required string mec { get; set; }


        public required string password { get; set; }


        public string? address { get; set; }
    }
}

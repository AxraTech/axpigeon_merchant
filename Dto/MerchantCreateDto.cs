using System.ComponentModel.DataAnnotations;

namespace AxpigeonApp.Dto
{
    public class MerchantCreateDto
    {
        public required string name { get; set; }


        public required string email { get; set; }
       
        public required string phone { get; set; }

      
        public required string password { get; set; }


        public string? address { get; set; }

    }
}

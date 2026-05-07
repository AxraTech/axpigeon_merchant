using Microsoft.AspNetCore.Http;
using System;

namespace AxpigeonApp.Dto
{
    public class SendMessageDto
    {
        public Guid id { get; set; }

        public string? phone { get; set; }

        public string message { get; set; }

        public bool isBulk { get; set; }


        public IFormFile? excelFile { get; set; }
    }
}

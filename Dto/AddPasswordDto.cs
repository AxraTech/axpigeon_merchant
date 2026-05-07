namespace AxpigeonApp.Dto
{
    public class AddPasswordDto
    {
        public required Guid branch_id { get; set; }

        public required string password { get; set; }
    }
}

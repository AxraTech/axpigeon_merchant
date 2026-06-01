namespace AxpigeonApp.Dto
{
    public class SetupPassphraseDto
    {
        public required Guid branch_id { get; set; }
        public required string passphrase { get; set; }
    }
}

namespace AxpigeonApp.Repository
{
    public interface IAuditRepository
    {
        Task LogDecryptAttempt(Guid userId, Guid? branchId, string action, string? ipAddress, string? userAgent);
    }
}

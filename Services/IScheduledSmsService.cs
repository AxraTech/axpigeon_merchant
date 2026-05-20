using AxpigeonApp.Dao;
using AxpigeonApp.Dto;

namespace AxpigeonApp.Services
{
    public interface IScheduledSmsService
    {
        Task<List<BrandNames>> GetAllBrandNames(Guid userId);
        Task ScheduleSingleAsync(ScheduleMessageDto dto);
        Task ScheduleBulkAsync(ScheduleMessageDto dto);
        Task<ScheduledSmsListResponseDao> GetScheduledListAsync(Guid userId, string? status, int page, int size);
        Task<ScheduledSmsResponseDao> GetScheduleDetailAsync(Guid userId, Guid scheduleId);
        Task<ScheduledSmsResponseDao> CancelScheduleAsync(Guid userId, Guid scheduleId, string? reason);
    }
}

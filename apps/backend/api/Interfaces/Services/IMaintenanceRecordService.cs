using api.DTO.Maintenance;

namespace api.Interfaces.Services;

public interface IMaintenanceRecordService
{
    Task<IReadOnlyList<MaintenanceRecordListItemResponse>> ListAsync(MaintenanceRecordFilter filter, CancellationToken cancellationToken);
    Task<MaintenanceRecordResponse> GetAsync(Guid recordId, CancellationToken cancellationToken);
    Task<MaintenanceRecordCreatedResponse> CreateAsync(CreateMaintenanceRecordRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid recordId, UpdateMaintenanceRecordRequest request, CancellationToken cancellationToken);
    Task CancelAsync(Guid recordId, CancellationToken cancellationToken);
}

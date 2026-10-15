using api.DTO.Passengers;

namespace api.Interfaces.Services;

// Passenger records managed by dispatch staff.
public interface IPassengerService
{
    Task<IReadOnlyList<PassengerListItemResponse>> ListAsync(PassengerFilter filter, CancellationToken cancellationToken);
    Task<PassengerResponse> GetAsync(Guid passengerId, CancellationToken cancellationToken);
    Task<PassengerCreatedResponse> CreateAsync(CreatePassengerRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid passengerId, UpdatePassengerRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid passengerId, CancellationToken cancellationToken);
    Task RestoreAsync(Guid passengerId, CancellationToken cancellationToken);
    Task ResetPasswordAsync(Guid passengerId, ResetPassengerPasswordRequest request, CancellationToken cancellationToken);
    Task<WalletTopUpResponse> TopUpWalletAsync(Guid passengerId, TopUpWalletRequest request, CancellationToken cancellationToken);
}

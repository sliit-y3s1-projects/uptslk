using api.DTO.Passengers;
using api.Entities;

namespace api.Interfaces.Repositories;

public interface IPassengerRepository : IRepository<Passenger>
{
    Task<List<Passenger>> SearchAsync(PassengerFilter filter, CancellationToken cancellationToken = default);

    // Loads the wallet with its transactions and the bookings with their trip route.
    Task<Passenger?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Passenger?> GetWithUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Passenger?> GetWithWalletAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid?> GetUserIdAsync(Guid passengerId, CancellationToken cancellationToken = default);
    Task<Passenger?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Guid?> GetIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Passenger?> GetActiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid?> GetActiveIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> PhoneNumberExistsAsync(string phoneNumber, Guid? exceptPassengerId = null, CancellationToken cancellationToken = default);
}

using api.DTO.Fares;

namespace api.Interfaces.Services;

public interface IFareRuleService
{
    Task<FareQuoteResponse> QuoteAsync(Guid tripId, Guid passengerId, CancellationToken cancellationToken);
    Task<FareQuoteResponse> QuoteForCurrentUserAsync(Guid tripId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FareRuleListItemResponse>> ListAsync(FareRuleFilter filter, CancellationToken cancellationToken);
    Task<FareRuleResponse> GetAsync(Guid fareRuleId, CancellationToken cancellationToken);
    Task<FareRuleCreatedResponse> CreateAsync(CreateFareRuleRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid fareRuleId, UpdateFareRuleRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid fareRuleId, CancellationToken cancellationToken);
}

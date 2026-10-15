using api.Common.Helpers;
using api.DTO.Support;
using api.Entities;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services;

public sealed class SupportRequestService(
    ISupportRequestRepository supportRequests,
    IPassengerRepository passengers,
    ITripRepository trips,
    IUnitOfWork unitOfWork) : ISupportRequestService
{
    private const string NotFoundMessage = "Support request not found.";

    public async Task<IReadOnlyList<SupportRequestResponse>> ListAsync(SupportRequestFilter filter, CancellationToken cancellationToken)
    {
        var requests = await supportRequests.SearchAsync(filter, cancellationToken);
        return requests.Select(SupportRequestResponse.From).ToList();
    }

    public async Task<SupportRequestResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var request = await supportRequests.GetWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);
        return SupportRequestResponse.From(request);
    }

    public async Task<SupportRequestCreatedResponse> CreateAsync(CreateSupportRequest input, CancellationToken cancellationToken)
    {
        if (input.PassengerId.HasValue && !await passengers.ExistsAsync(input.PassengerId.Value, cancellationToken))
            throw new BadRequestException("The selected passenger does not exist.");
        if (input.TripId.HasValue && !await trips.ExistsAsync(input.TripId.Value, cancellationToken))
            throw new BadRequestException("The selected trip does not exist.");

        var request = new SupportRequest
        {
            PassengerId = input.PassengerId,
            TripId = input.TripId,
            CentreId = input.CentreId,
            Type = input.Type,
            Priority = input.Priority,
            Subject = input.Subject.Trim(),
            Description = input.Description.Trim(),
            AssignedTo = Text.CleanOptional(input.AssignedTo)
        };
        supportRequests.Add(request);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new SupportRequestCreatedResponse(request.Id, request.Status);
    }

    public async Task UpdateAsync(Guid id, UpdateSupportRequest input, CancellationToken cancellationToken)
    {
        var request = await supportRequests.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);

        request.Priority = input.Priority;
        request.Status = input.Status;
        request.Subject = input.Subject.Trim();
        request.Description = input.Description.Trim();
        request.AssignedTo = Text.CleanOptional(input.AssignedTo);
        request.Resolution = Text.CleanOptional(input.Resolution);
        request.ResolvedAt = input.Status is SupportRequestStatus.Resolved or SupportRequestStatus.Closed ? DateTime.UtcNow : null;
        request.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

using api.Common.Helpers;
using api.DTO.Centres;
using api.Entities;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services;

public sealed class CentreService(
    ICentreRepository centres,
    IBayRepository bays,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : ICentreService
{
    private const string CentreNotFound = "Centre not found.";
    private const string BayNotFound = "Bay not found.";
    private const string DuplicateBayCode = "A bay with this code already exists at this centre.";

    public async Task<IReadOnlyList<CentreListItemResponse>> ListAsync(CentreFilter filter, CancellationToken cancellationToken)
    {
        var found = await centres.SearchAsync(filter, cancellationToken);
        return found.Select(CentreListItemResponse.From).ToList();
    }

    public async Task<CentreResponse> GetAsync(Guid centreId, CancellationToken cancellationToken)
    {
        var centre = await centres.GetWithBaysAndRoutesAsync(centreId, cancellationToken)
            ?? throw new NotFoundException(CentreNotFound);
        return CentreResponse.From(centre);
    }

    public async Task<CentreCreatedResponse> CreateAsync(CreateCentreRequest request, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.Code);
        if (await centres.CodeExistsAsync(code, cancellationToken))
            throw new ConflictException("A centre with this code already exists.");

        var centre = new Centre
        {
            Code = code,
            Name = request.Name.Trim(),
            City = request.City.Trim(),
            District = request.District.Trim(),
            Description = Text.CleanOptional(request.Description),
            Status = request.Status
        };
        centres.Add(centre);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CentreCreatedResponse(centre.Id, centre.Code, centre.Name, centre.Status);
    }

    public async Task UpdateAsync(Guid centreId, UpdateCentreRequest request, CancellationToken cancellationToken)
    {
        var centre = await GetManagedCentreAsync(centreId, cancellationToken);
        centre.Name = request.Name.Trim();
        centre.City = request.City.Trim();
        centre.District = request.District.Trim();
        centre.Description = Text.CleanOptional(request.Description);
        centre.Status = request.Status;
        centre.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CloseAsync(Guid centreId, CancellationToken cancellationToken)
    {
        var centre = await GetManagedCentreAsync(centreId, cancellationToken);
        centre.Status = CentreStatus.Closed;
        centre.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BayResponse>> ListBaysAsync(Guid centreId, CancellationToken cancellationToken)
    {
        if (!await centres.ExistsAsync(centreId, cancellationToken)) throw new NotFoundException(CentreNotFound);
        var found = await bays.ListForCentreAsync(centreId, cancellationToken);
        return found.Select(BayResponse.From).ToList();
    }

    public async Task<BayResponse> GetBayAsync(Guid bayId, CancellationToken cancellationToken)
    {
        var bay = await bays.GetReadOnlyAsync(bayId, cancellationToken) ?? throw new NotFoundException(BayNotFound);
        return BayResponse.From(bay);
    }

    public async Task<BayResponse> CreateBayAsync(Guid centreId, CreateBayRequest request, CancellationToken cancellationToken)
    {
        if (!await centres.ExistsAsync(centreId, cancellationToken)) throw new NotFoundException(CentreNotFound);
        currentUser.EnsureCanManageCentre(centreId);

        var code = NormalizeCode(request.Code);
        if (await bays.CodeExistsAsync(centreId, code, cancellationToken: cancellationToken)) throw new ConflictException(DuplicateBayCode);

        var bay = new Bay { CentreId = centreId, Code = code, Name = Text.CleanOptional(request.Name), Status = request.Status };
        bays.Add(bay);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return BayResponse.From(bay);
    }

    public async Task UpdateBayAsync(Guid bayId, UpdateBayRequest request, CancellationToken cancellationToken)
    {
        var bay = await GetManagedBayAsync(bayId, cancellationToken);
        var code = NormalizeCode(request.Code);
        if (await bays.CodeExistsAsync(bay.CentreId, code, exceptBayId: bayId, cancellationToken)) throw new ConflictException(DuplicateBayCode);

        bay.Code = code;
        bay.Name = Text.CleanOptional(request.Name);
        bay.Status = request.Status;
        bay.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateBayAsync(Guid bayId, CancellationToken cancellationToken)
    {
        var bay = await GetManagedBayAsync(bayId, cancellationToken);
        bay.Status = BayStatus.OutOfService;
        bay.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Centre> GetManagedCentreAsync(Guid centreId, CancellationToken cancellationToken)
    {
        var centre = await centres.GetByIdAsync(centreId, cancellationToken) ?? throw new NotFoundException(CentreNotFound);
        currentUser.EnsureCanManageCentre(centre.Id);
        return centre;
    }

    private async Task<Bay> GetManagedBayAsync(Guid bayId, CancellationToken cancellationToken)
    {
        var bay = await bays.GetByIdAsync(bayId, cancellationToken) ?? throw new NotFoundException(BayNotFound);
        currentUser.EnsureCanManageCentre(bay.CentreId);
        return bay;
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
}

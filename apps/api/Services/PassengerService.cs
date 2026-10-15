using api.Common.Extensions;
using api.Common.Helpers;
using api.DTO.Passengers;
using api.Entities;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public sealed class PassengerService(
    IPassengerRepository passengers,
    IWalletRepository wallets,
    IUnitOfWork unitOfWork,
    UserManager<User> userManager) : IPassengerService
{
    private const string NotFoundMessage = "Passenger not found.";
    private const string DuplicatePhone = "A passenger with this phone number already exists.";

    public async Task<IReadOnlyList<PassengerListItemResponse>> ListAsync(PassengerFilter filter, CancellationToken cancellationToken)
    {
        var found = await passengers.SearchAsync(filter, cancellationToken);
        return found.Select(PassengerListItemResponse.From).ToList();
    }

    public async Task<PassengerResponse> GetAsync(Guid passengerId, CancellationToken cancellationToken)
    {
        var passenger = await passengers.GetWithDetailsAsync(passengerId, cancellationToken) ?? throw new NotFoundException(NotFoundMessage);
        return PassengerResponse.From(passenger);
    }

    public async Task<PassengerCreatedResponse> CreateAsync(CreatePassengerRequest request, CancellationToken cancellationToken)
    {
        var phoneNumber = NormalizePhone(request.PhoneNumber);
        if (await passengers.PhoneNumberExistsAsync(phoneNumber, cancellationToken: cancellationToken)) throw new ConflictException(DuplicatePhone);

        var createPortalAccount = !string.IsNullOrWhiteSpace(request.Password);
        if (createPortalAccount && string.IsNullOrWhiteSpace(request.Email))
            throw new BadRequestException("An email address is required when creating portal credentials.");

        var user = createPortalAccount ? await CreatePortalAccountAsync(request) : null;

        var passenger = new Passenger
        {
            UserId = user?.Id,
            FullName = request.FullName.Trim(),
            PhoneNumber = phoneNumber,
            Email = Text.CleanOptional(request.Email),
            Category = request.Category
        };
        passenger.Wallet = new Wallet { Passenger = passenger };
        passengers.Add(passenger);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Do not leave a login account behind without its passenger profile.
            if (user is not null) await userManager.DeleteAsync(user);
            throw new ConflictException("Unable to create the passenger profile. Check the supplied details.");
        }

        return new PassengerCreatedResponse(passenger.Id, passenger.FullName, passenger.PhoneNumber, passenger.Category, passenger.Wallet.Balance, user is not null);
    }

    public async Task UpdateAsync(Guid passengerId, UpdatePassengerRequest request, CancellationToken cancellationToken)
    {
        var passenger = await passengers.GetByIdAsync(passengerId, cancellationToken) ?? throw new NotFoundException(NotFoundMessage);

        var phoneNumber = NormalizePhone(request.PhoneNumber);
        if (await passengers.PhoneNumberExistsAsync(phoneNumber, passengerId, cancellationToken)) throw new ConflictException(DuplicatePhone);

        passenger.FullName = request.FullName.Trim();
        passenger.PhoneNumber = phoneNumber;
        passenger.Email = Text.CleanOptional(request.Email);
        passenger.Category = request.Category;
        passenger.IsActive = request.IsActive;
        passenger.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid passengerId, CancellationToken cancellationToken)
    {
        var passenger = await passengers.GetByIdAsync(passengerId, cancellationToken) ?? throw new NotFoundException(NotFoundMessage);
        passenger.IsActive = false;
        passenger.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreAsync(Guid passengerId, CancellationToken cancellationToken)
    {
        var passenger = await passengers.GetWithUserAsync(passengerId, cancellationToken) ?? throw new NotFoundException(NotFoundMessage);
        passenger.IsActive = true;
        if (passenger.User is not null) passenger.User.IsActive = true;
        passenger.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(Guid passengerId, ResetPassengerPasswordRequest request, CancellationToken cancellationToken)
    {
        var passenger = await passengers.GetWithUserAsync(passengerId, cancellationToken) ?? throw new NotFoundException(NotFoundMessage);
        if (passenger.User is null) throw new BadRequestException("This passenger does not have a portal account yet.");

        var token = await userManager.GeneratePasswordResetTokenAsync(passenger.User);
        (await userManager.ResetPasswordAsync(passenger.User, token, request.Password)).ThrowIfFailed();
    }

    public async Task<WalletTopUpResponse> TopUpWalletAsync(Guid passengerId, TopUpWalletRequest request, CancellationToken cancellationToken)
    {
        var walletId = await wallets.GetIdByPassengerIdAsync(passengerId, cancellationToken) ?? throw new NotFoundException("Wallet not found.");

        // Lock the wallet so parallel top-ups add up instead of overwriting each other.
        await using var dbTransaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var wallet = await wallets.LockForUpdateAsync(walletId, cancellationToken);
        wallet.Balance += request.Amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        var transaction = new Transaction { WalletId = wallet.Id, Type = TransactionType.Topup, Amount = request.Amount };
        wallets.AddTransaction(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        return new WalletTopUpResponse(transaction.Id, wallet.Balance, transaction.Type, transaction.Amount, transaction.CreatedAt);
    }

    private async Task<User> CreatePortalAccountAsync(CreatePassengerRequest request)
    {
        var email = request.Email!.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
            throw new ConflictException("An account with this email address already exists.");

        var user = new User { UserName = email, Email = email, Name = request.FullName.Trim(), Role = UserRole.Commuter };
        (await userManager.CreateAsync(user, request.Password!)).ThrowIfFailed();
        return user;
    }

    private static string NormalizePhone(string value) => value.Trim().Replace(" ", string.Empty).Replace("-", string.Empty);
}

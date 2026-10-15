using api.Common.Constants;
using api.DTO.Bookings;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/v1/bookings")]
public class BookingsController(IBookingService bookings) : ControllerBase
{
    [Authorize(Roles = AppRoles.Commuter)]
    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<TicketResponse>>> ListForCurrentUser(CancellationToken cancellationToken) =>
        Ok(await bookings.ListTicketsForCurrentUserAsync(cancellationToken));

    [Authorize(Roles = AppRoles.DispatchStaff)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookingListItemResponse>>> List([FromQuery] BookingFilter filter, CancellationToken cancellationToken) =>
        Ok(await bookings.ListAsync(filter, cancellationToken));

    [Authorize(Roles = AppRoles.DispatchStaff)]
    [HttpGet("{bookingId:guid}")]
    public async Task<ActionResult<BookingResponse>> Get(Guid bookingId, CancellationToken cancellationToken) =>
        Ok(await bookings.GetAsync(bookingId, cancellationToken));

    [Authorize(Roles = AppRoles.DispatchStaff)]
    [HttpPost]
    public async Task<ActionResult<BookingCreatedResponse>> Create(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var created = await bookings.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { bookingId = created.Id }, created);
    }

    [HttpPost("me")]
    [Authorize(Roles = AppRoles.Commuter)]
    public async Task<ActionResult<BookingCreatedResponse>> CreateForCurrentUser(CreateMyBookingRequest request, CancellationToken cancellationToken)
    {
        var created = await bookings.CreateForCurrentUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { bookingId = created.Id }, created);
    }

    [Authorize(Roles = AppRoles.DispatchStaff)]
    [HttpPatch("{bookingId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid bookingId, UpdateBookingStatusRequest request, CancellationToken cancellationToken)
    {
        await bookings.CompleteAsync(bookingId, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.DispatchStaff)]
    [HttpDelete("{bookingId:guid}")]
    public async Task<IActionResult> Cancel(Guid bookingId, CancelBookingRequest request, CancellationToken cancellationToken)
    {
        await bookings.CancelAsync(bookingId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("trips/{tripId:guid}/seats")]
    public async Task<ActionResult<SeatAvailabilityResponse>> Seats(Guid tripId, CancellationToken cancellationToken) =>
        Ok(await bookings.GetSeatAvailabilityAsync(tripId, cancellationToken));

    [Authorize(Roles = AppRoles.DispatchStaff)]
    [HttpGet("trips/{tripId:guid}/manifest")]
    public async Task<ActionResult<TripManifestResponse>> Manifest(Guid tripId, CancellationToken cancellationToken) =>
        Ok(await bookings.GetManifestAsync(tripId, cancellationToken));
}

using api.Enums;
using api.Models;

namespace api.Services;

public static class BookingEligibility
{
    // Keep genuinely ongoing journeys visible, but do not let stale statuses
    // leave tickets in Upcoming indefinitely. This does not complete the trip.
    public static DateTime ActiveUntil(Trip trip)
    {
        var duration = trip.RouteDirection?.EstimatedDurationMin ?? 0;
        if (duration <= 0) duration = trip.Route?.EstimatedDurationMin ?? 0;
        if (duration <= 0) duration = 60;
        return (trip.ActualDepartureAt ?? trip.ScheduledTime).AddMinutes((double)duration + 60);
    }

    public static bool IsOpenForBooking(Trip trip, DateTime now) =>
        trip.ScheduledTime > now &&
        trip.Status is TripStatus.Scheduled or TripStatus.Ready or TripStatus.Boarding;

    public static bool CanBoard(Booking booking, DateTime now) =>
        booking.Status == BookingStatus.Confirmed &&
        (IsOpenForBooking(booking.Trip, now) ||
         (booking.Trip.Status is TripStatus.Boarding or TripStatus.Delayed &&
          now < ActiveUntil(booking.Trip)));

    public static string TicketGroup(Booking booking, DateTime now)
    {
        if (booking.Status == BookingStatus.Cancelled || booking.Trip.Status == TripStatus.Cancelled)
            return "Cancelled";
        if (booking.Status == BookingStatus.Completed || booking.Trip.Status == TripStatus.Completed)
            return "Past";
        return booking.Trip.ScheduledTime > now ||
            (booking.Trip.Status is TripStatus.Boarding or TripStatus.Delayed or TripStatus.Dispatched &&
             now < ActiveUntil(booking.Trip))
                ? "Upcoming" : "Past";
    }
}

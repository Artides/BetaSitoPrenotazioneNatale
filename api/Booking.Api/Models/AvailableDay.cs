namespace Booking.Api.Models;
public sealed record AvailableDay(string Date, IReadOnlyList<AvailableSlot> Slots);

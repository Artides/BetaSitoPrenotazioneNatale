namespace Booking.Api.Models;
public sealed record AvailabilityResponse(string Timezone, int SlotDurationMinutes, int BreakMinutes, IReadOnlyList<AvailableDay> Days);

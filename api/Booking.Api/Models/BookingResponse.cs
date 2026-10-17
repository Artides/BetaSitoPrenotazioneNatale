namespace Booking.Api.Models;
public sealed record BookingResponse(bool Success, string BookingId, DateTimeOffset Start, DateTimeOffset End, bool EmailSent = false);
public sealed record CalendarEvent(string Id, DateTimeOffset Start, DateTimeOffset End, string? Fingerprint = null, string? BookingId = null);
public sealed class ApiException(int status, string code) : Exception(code) { public int Status { get; } = status; public string Code { get; } = code; }

using System.Text.Json.Serialization;
namespace Booking.Api.Models;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record BookingRequest(string Start, Customer Customer, string? CaptchaToken = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record Customer(string Name, string Email, string? Phone = null);

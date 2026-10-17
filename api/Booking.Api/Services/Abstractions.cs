using Booking.Api.Models;
namespace Booking.Api.Services;
public interface IEmailService { Task<bool> SendAsync(BookingRequest request,BookingResponse booking,CancellationToken ct); }

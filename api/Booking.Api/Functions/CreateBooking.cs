using Booking.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
namespace Booking.Api.Functions;
public sealed class CreateBooking(ApiEndpoints endpoints) {
    [Function("CreateBooking")]
    public Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous,"post","options",Route="book")] HttpRequest request) => endpoints.BookAsync(request);
}

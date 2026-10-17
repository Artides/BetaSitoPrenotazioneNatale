using Booking.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
namespace Booking.Api.Functions;
public sealed class GetAvailability(ApiEndpoints endpoints) {
    [Function("GetAvailability")]
    public Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous,"get","options",Route="availability")] HttpRequest request) => endpoints.AvailabilityAsync(request);
}

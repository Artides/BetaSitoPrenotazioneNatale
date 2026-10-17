using System.Collections.Concurrent;
using Booking.Api.Models;
namespace Booking.Api.Services;
// Explicit local demo only. Never selected by the Functions host or deployed Azure configuration.
public sealed class LocalCalendarService : ICalendarService {
    private readonly ConcurrentDictionary<string,CalendarEvent> events=new();
    public Task<IReadOnlyList<CalendarEvent>> ListAsync(DateTimeOffset from,DateTimeOffset to,CancellationToken ct) => Task.FromResult<IReadOnlyList<CalendarEvent>>(events.Values.Where(e=>e.Start<to&&e.End>from).ToArray());
    public Task<CalendarEvent?> FindAsync(string id,CancellationToken ct) => Task.FromResult(events.GetValueOrDefault(id));
    public Task<CalendarEvent> CreateAsync(string id,string bookingId,string fingerprint,BookingRequest request,DateTimeOffset start,DateTimeOffset end,CancellationToken ct) {
        var value=new CalendarEvent(id,start,end,fingerprint,bookingId);
        return Task.FromResult(events.GetOrAdd(id,value));
    }
}
public sealed class LocalEmailService : IEmailService { public Task<bool> SendAsync(BookingRequest request,BookingResponse booking,CancellationToken ct) => Task.FromResult(false); }


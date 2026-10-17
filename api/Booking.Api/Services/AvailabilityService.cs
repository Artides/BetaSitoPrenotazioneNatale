using Booking.Api.Configuration;
using Booking.Api.Models;
namespace Booking.Api.Services;
public sealed class AvailabilityService(BookingOptions options, ICalendarService calendar, TimeProvider clock)
{
    public IEnumerable<DateTimeOffset> Candidates(DateOnly day) {
        if(day<options.StartDate || day>options.EndDate) yield break;
        foreach(var (begin,end) in new[]{(options.MorningStart,options.MorningEnd),(options.AfternoonStart,options.AfternoonEnd)}) {
            var stop=options.At(day,end);
            for(var start=options.At(day,begin);start.AddMinutes(options.DurationMinutes)<=stop;start=start.AddMinutes(30))
                if(start>clock.GetUtcNow()) yield return start;
        }
    }
    public bool IsValid(DateTimeOffset start) => Candidates(DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(start,options.Zone).DateTime)).Contains(start);
    public bool IsFree(DateTimeOffset start,IEnumerable<CalendarEvent> events) => !events.Any(e => start < e.End.AddMinutes(options.BreakMinutes) && start.AddMinutes(options.DurationMinutes+options.BreakMinutes)>e.Start);
    public async Task<AvailabilityResponse> GetAsync(DateOnly? from,DateOnly? to,CancellationToken ct) {
        if(from.HasValue && to.HasValue && from>to) throw new ApiException(400,"INVALID_RANGE");
        var first=from>options.StartDate?from.Value:options.StartDate;
        var last=to<options.EndDate?to.Value:options.EndDate;
        var days=new List<AvailableDay>();
        if(first<=last) {
            var events=await calendar.ListAsync(options.At(first,TimeOnly.MinValue).AddMinutes(-options.BreakMinutes),options.At(last.AddDays(1),TimeOnly.MinValue).AddMinutes(options.BreakMinutes),ct);
            for(var day=first;day<=last;day=day.AddDays(1))
                days.Add(new(day.ToString("yyyy-MM-dd"),Candidates(day).Where(s=>IsFree(s,events)).Select(s=>new AvailableSlot(s,s.ToString("HH:mm"))).ToArray()));
        }
        return new(options.TimeZone,options.DurationMinutes,options.BreakMinutes,days);
    }
}

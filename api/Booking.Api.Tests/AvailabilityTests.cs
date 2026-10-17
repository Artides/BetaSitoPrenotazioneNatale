using Booking.Api.Configuration;
using Booking.Api.Models;
using Booking.Api.Services;
using Xunit;
namespace Booking.Api.Tests;
public sealed class AvailabilityTests {
    private readonly BookingOptions rules=new();
    private readonly FakeCalendar calendar=new();
    private AvailabilityService Service => new(rules,calendar,new FixedClock());
    private DateTimeOffset At(int hour,int minute=0) => rules.At(new(2026,11,10),new(hour,minute));
    [Fact] public async Task EmptyCalendarReturnsAllElevenCandidates() {
        var result=await Service.GetAsync(new(2026,11,10),new(2026,11,10),default);
        Assert.Equal(11,result.Days.Single().Slots.Count);
        Assert.DoesNotContain(result.Days[0].Slots,s=>s.Label is "13:00" or "13:30" or "16:30");
    }
    [Theory]
    [InlineData(10,0,10,30,"11:00")]
    [InlineData(10,30,11,0,"11:30")]
    [InlineData(12,30,13,0,"14:00")]
    [InlineData(14,0,14,30,"15:00")]
    [InlineData(16,0,16,30,"15:00")]
    [InlineData(10,0,11,0,"11:30")]
    public async Task RealEventAndVirtualBreak(int h,int m,int eh,int em,string available) {
        calendar.Events.Add(new("manual",At(h,m),At(eh,em)));
        var result=await Service.GetAsync(new(2026,11,10),new(2026,11,10),default);
        Assert.DoesNotContain(result.Days[0].Slots,s=>s.Start==At(h,m));
        Assert.Contains(result.Days[0].Slots,s=>s.Label==available);
        Assert.All(result.Days[0].Slots,s=>Assert.True(s.Start>=At(eh,em).AddMinutes(30)||s.Start.AddMinutes(60)<=At(h,m)));
        Assert.Single(calendar.Events);Assert.Equal(At(eh,em),calendar.Events[0].End);
    }
    [Fact] public async Task ElevenOClockAlsoBlocksTenThirty() {
        calendar.Events.Add(new("manual",At(11),At(11,30)));
        var result=await Service.GetAsync(new(2026,11,10),new(2026,11,10),default);
        Assert.DoesNotContain(result.Days[0].Slots,s=>s.Label is "10:30" or "11:00" or "11:30");
        Assert.Contains(result.Days[0].Slots,s=>s.Label=="10:00");Assert.Contains(result.Days[0].Slots,s=>s.Label=="12:00");
    }
    [Fact] public async Task ManualMoveIsReflectedWithoutCache() {
        calendar.Events.Add(new("manual",At(10),At(10,30)));
        Assert.False(Service.IsFree(At(10,30),calendar.Events));
        calendar.Events[0]=calendar.Events[0] with {Start=At(11,30),End=At(12)};
        var result=await Service.GetAsync(new(2026,11,10),new(2026,11,10),default);
        Assert.Contains(result.Days[0].Slots,s=>s.Label=="10:30");
        Assert.DoesNotContain(result.Days[0].Slots,s=>s.Label is "11:00" or "11:30" or "12:00");
        Assert.Contains(result.Days[0].Slots,s=>s.Label=="12:30");
    }
    [Fact] public async Task OutOfHoursEventStillAppliesBuffer() {
        calendar.Events.Add(new("outside",At(9),At(9,45)));
        var result=await Service.GetAsync(new(2026,11,10),new(2026,11,10),default);
        Assert.DoesNotContain(result.Days[0].Slots,s=>s.Label=="10:00");
        Assert.Contains(result.Days[0].Slots,s=>s.Label=="10:30");
    }
    [Theory][InlineData(11,6,false)][InlineData(11,7,true)][InlineData(12,5,true)][InlineData(12,6,false)]
    public void InclusiveSeasonBoundaries(int month,int day,bool valid) => Assert.Equal(valid,Service.IsValid(rules.At(new(2026,month,day),new(10,0))));
    [Fact] public async Task RangeIsClampedAndEmptyOutsideSeason() {
        var all=await Service.GetAsync(new(2020,1,1),new(2030,1,1),default);Assert.Equal(29,all.Days.Count);
        Assert.Empty((await Service.GetAsync(new(2027,1,1),new(2027,1,2),default)).Days);
        Assert.Empty((await Service.GetAsync(new(2026,1,1),new(2026,1,2),default)).Days);
    }
    [Fact] public async Task FullDayEventBlocksAllSlots() {
        calendar.Events.Add(new("all-day",At(0),At(0).AddDays(1)));
        Assert.Empty((await Service.GetAsync(new(2026,11,10),new(2026,11,10),default)).Days[0].Slots);
    }
    [Fact] public void TimezoneUsesDaylightSaving() {
        Assert.Equal(TimeSpan.FromHours(2),rules.At(new(2026,7,1),new(10,0)).Offset);
        Assert.Equal(TimeSpan.FromHours(1),rules.At(new(2026,11,10),new(10,0)).Offset);
    }
    [Fact] public async Task PastCandidatesAreNeverOffered() {
        var service=new AvailabilityService(rules,calendar,new FixedClock(new DateTimeOffset(2027,1,1,0,0,0,TimeSpan.Zero)));
        Assert.All((await service.GetAsync(null,null,default)).Days,d=>Assert.Empty(d.Slots));
    }
    [Fact] public void MisalignedStartOrSecondsAreRejected() {Assert.False(Service.IsValid(At(10,15)));Assert.False(Service.IsValid(At(10).AddSeconds(1)));}
}
internal sealed class FixedClock(DateTimeOffset? now=null) : TimeProvider { public override DateTimeOffset GetUtcNow()=>now??new(2026,10,15,0,0,0,TimeSpan.Zero); }
internal sealed class FakeCalendar : ICalendarService {
    public List<CalendarEvent> Events {get;}=[];
    public int Creates;
    public Task<IReadOnlyList<CalendarEvent>> ListAsync(DateTimeOffset from,DateTimeOffset to,CancellationToken ct) => Task.FromResult<IReadOnlyList<CalendarEvent>>(Events.Where(e=>e.End>from&&e.Start<to).ToArray());
    public Task<CalendarEvent?> FindAsync(string id,CancellationToken ct)=>Task.FromResult(Events.FirstOrDefault(e=>e.Id==id));
    public Task<CalendarEvent> CreateAsync(string id,string bookingId,string fingerprint,BookingRequest request,DateTimeOffset start,DateTimeOffset end,CancellationToken ct) {Creates++;var e=new CalendarEvent(id,start,end,fingerprint,bookingId);Events.Add(e);return Task.FromResult(e);}
}


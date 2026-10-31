using Booking.Api.Configuration;
using Booking.Api.Models;
using Booking.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
namespace Booking.Api.Tests;
public sealed class BookingTests {
    private readonly FakeCalendar calendar=new();private readonly BookingOptions rules=new();
    private BookingService Service => new(rules,new(rules,calendar,new FixedClock()),calendar,new BrokenEmail(),new NoCaptcha(),NullLogger<BookingService>.Instance);
    private BookingRequest Request(string start="2026-11-10T10:00:00+01:00")=>new(start,new("Mario Rossi","mario@example.com","+393331234567"));
    [Fact] public async Task CreatesOnlyRealThirtyMinuteEventEvenIfEmailFails() {
        var result=await Service.BookAsync(Request(),Guid.NewGuid().ToString(),default);
        Assert.True(result.Success);Assert.False(result.EmailSent);Assert.Single(calendar.Events);Assert.Equal(TimeSpan.FromMinutes(30),calendar.Events[0].End-calendar.Events[0].Start);
        Assert.NotEqual(calendar.Events[0].Id,result.BookingId);
    }
    [Fact] public async Task RetryWithSameKeyReturnsOriginalWithoutDuplicates() {
        var key=Guid.NewGuid().ToString();var service=Service;
        var a=await service.BookAsync(Request(),key,default);var b=await service.BookAsync(Request(),key,default);
        Assert.Equal(a.BookingId,b.BookingId);Assert.Equal(1,calendar.Creates);
    }
    [Fact] public async Task SameKeyDifferentPayloadConflicts() {
        var key=Guid.NewGuid().ToString();await Service.BookAsync(Request(),key,default);
        var ex=await Assert.ThrowsAsync<ApiException>(()=>Service.BookAsync(Request() with {Customer=new("Other","other@example.com")},key,default));Assert.Equal("IDEMPOTENCY_CONFLICT",ex.Code);
    }
    [Theory][InlineData("2026-11-10T10:30:00+01:00")][InlineData("2026-11-10T11:00:00+01:00")][InlineData("2026-11-10T11:30:00+01:00")]
    public async Task FinalCheckRejectsOccupiedAndAdjacentSlots(string start) {
        calendar.Events.Add(new("manual",DateTimeOffset.Parse("2026-11-10T11:00:00+01:00"),DateTimeOffset.Parse("2026-11-10T11:30:00+01:00")));
        var ex=await Assert.ThrowsAsync<ApiException>(()=>Service.BookAsync(Request(start),Guid.NewGuid().ToString(),default));Assert.Equal(409,ex.Status);Assert.Equal(0,calendar.Creates);
    }
    [Theory][InlineData("2026-11-10T13:00:00+01:00")][InlineData("2026-11-10T16:30:00+01:00")][InlineData("2026-11-06T10:00:00+01:00")][InlineData("2026-12-06T10:00:00+01:00")][InlineData("2026-11-10T10:15:00+01:00")][InlineData("2026-11-10T10:00:00")]
    public async Task InvalidSlotsNeverCreate(string start) {var ex=await Assert.ThrowsAsync<ApiException>(()=>Service.BookAsync(Request(start),Guid.NewGuid().ToString(),default));Assert.Equal(400,ex.Status);Assert.Equal(0,calendar.Creates);}
    [Fact] public void RejectsControlCharactersAndDisplayNameEmail() {
        Assert.Throws<ApiException>(()=>BookingService.Validate(Request() with {Customer=new("Mario\nInjected","a@example.com")}));
        Assert.Throws<ApiException>(()=>BookingService.Validate(Request() with {Customer=new("Mario","Mario <a@example.com>")}));
    }
    [Fact] public async Task FinalCheckRejectsAlreadyBookedSlotWithDifferentKey() {
        var service = Service;
        await service.BookAsync(Request(), Guid.NewGuid().ToString(), default);
        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            service.BookAsync(Request(), Guid.NewGuid().ToString(), default));
        Assert.Equal("SLOT_NOT_AVAILABLE", ex.Code);
        Assert.Equal(1, calendar.Creates);
    }

    [Theory]
    [InlineData("2026-11-08T10:00:00+01:00")]
    [InlineData("2026-11-15T14:00:00+01:00")]
    [InlineData("2026-11-22T16:00:00+01:00")]
    [InlineData("2026-11-29T12:30:00+01:00")]
    [InlineData("2026-11-08T09:00:00Z")]
    [InlineData("2026-11-07T23:30:00-10:00")]
    [InlineData("2026-11-09T00:00:00+14:00")]
    public async Task SundaysInRomeAreRejectedBeforeAnyCalendarAccess(string start) {
        var unavailable=new UnavailableCalendar();
        var service=new BookingService(rules,new(rules,unavailable,new FixedClock()),unavailable,
            new BrokenEmail(),new NoCaptcha(),NullLogger<BookingService>.Instance);
        var ex=await Assert.ThrowsAsync<ApiException>(()=>service.BookAsync(Request(start),Guid.NewGuid().ToString(),default));
        Assert.Equal(400,ex.Status);
        Assert.Equal("INVALID_SLOT",ex.Code);
    }

    [Fact] public async Task MondayInRomeRemainsBookableWithDifferentClientOffset() {
        var result=await Service.BookAsync(Request("2026-11-08T23:00:00-10:00"),Guid.NewGuid().ToString(),default);
        Assert.True(result.Success);
        Assert.Equal(DayOfWeek.Monday,TimeZoneInfo.ConvertTime(result.Start,rules.Zone).DayOfWeek);
        Assert.Equal(1,calendar.Creates);
    }

    private sealed class UnavailableCalendar:ICalendarService {
        public Task<IReadOnlyList<CalendarEvent>> ListAsync(DateTimeOffset from,DateTimeOffset to,CancellationToken ct)=>throw new InvalidOperationException("Calendar must not be contacted for a closed day.");
        public Task<CalendarEvent?> FindAsync(string id,CancellationToken ct)=>throw new InvalidOperationException("Calendar must not be contacted for a closed day, including retries.");
        public Task<CalendarEvent> CreateAsync(string id,string bookingId,string fingerprint,BookingRequest request,DateTimeOffset start,DateTimeOffset end,CancellationToken ct)=>throw new InvalidOperationException("Calendar must not be contacted for a closed day.");
    }
    private sealed class BrokenEmail:IEmailService {public Task<bool> SendAsync(BookingRequest r,BookingResponse b,CancellationToken ct)=>throw new InvalidOperationException("email down");}
    private sealed class NoCaptcha:ICaptchaService {public Task ValidateAsync(string? token,CancellationToken ct)=>Task.CompletedTask;}
}

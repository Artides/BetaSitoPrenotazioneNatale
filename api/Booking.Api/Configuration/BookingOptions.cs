using System.Globalization;
namespace Booking.Api.Configuration;
public sealed class BookingOptions
{
    public string TimeZone { get; init; } = "Europe/Rome";
    public DateOnly StartDate { get; init; } = new(2026,11,7);
    public DateOnly EndDate { get; init; } = new(2026,12,5);
    public TimeOnly MorningStart { get; init; } = new(10,0);
    public TimeOnly MorningEnd { get; init; } = new(13,0);
    public TimeOnly AfternoonStart { get; init; } = new(14,0);
    public TimeOnly AfternoonEnd { get; init; } = new(16,30);
    public int DurationMinutes { get; init; } = 30;
    public int BreakMinutes { get; init; } = 30;
    public TimeZoneInfo Zone => TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
    public DateTimeOffset At(DateOnly date, TimeOnly time) {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        if (Zone.IsInvalidTime(local) || Zone.IsAmbiguousTime(local)) throw new ArgumentException("Ambiguous local time");
        return new(local, Zone.GetUtcOffset(local));
    }
    public static BookingOptions Load(IConfiguration c) {
        string S(string key,string fallback) => c[key] ?? fallback;
        var o = new BookingOptions {
            TimeZone=S("BOOKING_TIME_ZONE","Europe/Rome"),
            StartDate=DateOnly.ParseExact(S("BOOKING_START_DATE","2026-11-07"),"yyyy-MM-dd",CultureInfo.InvariantCulture),
            EndDate=DateOnly.ParseExact(S("BOOKING_END_DATE","2026-12-05"),"yyyy-MM-dd",CultureInfo.InvariantCulture),
            MorningStart=TimeOnly.Parse(S("BOOKING_MORNING_START","10:00"),CultureInfo.InvariantCulture),
            MorningEnd=TimeOnly.Parse(S("BOOKING_MORNING_END","13:00"),CultureInfo.InvariantCulture),
            AfternoonStart=TimeOnly.Parse(S("BOOKING_AFTERNOON_START","14:00"),CultureInfo.InvariantCulture),
            AfternoonEnd=TimeOnly.Parse(S("BOOKING_AFTERNOON_END","16:30"),CultureInfo.InvariantCulture),
            DurationMinutes=int.Parse(S("BOOKING_DURATION_MINUTES","30")), BreakMinutes=int.Parse(S("BOOKING_BREAK_MINUTES","30"))
        };
        _=o.Zone;
        if(o.EndDate < o.StartDate || o.EndDate.DayNumber-o.StartDate.DayNumber>90 || o.DurationMinutes<1 || o.DurationMinutes>240 || o.BreakMinutes<0 || o.BreakMinutes>240 || o.MorningStart>=o.MorningEnd || o.AfternoonStart>=o.AfternoonEnd || o.MorningEnd>o.AfternoonStart) throw new InvalidOperationException("Invalid booking configuration");
        return o;
    }
}

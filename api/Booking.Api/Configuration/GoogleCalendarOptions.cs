namespace Booking.Api.Configuration;
public sealed record GoogleCalendarOptions(string CalendarId, string ClientId, string ClientSecret, string RefreshToken)
{
    public static GoogleCalendarOptions Load(IConfiguration c) => new(c["GOOGLE_CALENDAR_ID"] ?? "", c["GOOGLE_CLIENT_ID"] ?? "", c["GOOGLE_CLIENT_SECRET"] ?? "", c["GOOGLE_REFRESH_TOKEN"] ?? "");
    public bool Configured => new[]{CalendarId,ClientId,ClientSecret,RefreshToken}.All(x=>!string.IsNullOrWhiteSpace(x));
}

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Booking.Api.Configuration;
using Booking.Api.Models;
namespace Booking.Api.Services;
public interface ICalendarService {
    Task<IReadOnlyList<CalendarEvent>> ListAsync(DateTimeOffset from,DateTimeOffset to,CancellationToken ct);
    Task<CalendarEvent?> FindAsync(string eventId,CancellationToken ct);
    Task<CalendarEvent> CreateAsync(string eventId,string bookingId,string fingerprint,BookingRequest request,DateTimeOffset start,DateTimeOffset end,CancellationToken ct);
}
public sealed class GoogleCalendarService(HttpClient http,GoogleCalendarOptions config,BookingOptions booking) : ICalendarService
{
    private string Root => "https://www.googleapis.com/calendar/v3/calendars/"+Uri.EscapeDataString(config.CalendarId)+"/events";
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method,string url,object? body,CancellationToken ct) {
        if(!config.Configured) throw new ApiException(503,"CALENDAR_NOT_CONFIGURED");
        using var tokenResponse=await http.PostAsync("https://oauth2.googleapis.com/token",new FormUrlEncodedContent(new Dictionary<string,string>{["client_id"]=config.ClientId,["client_secret"]=config.ClientSecret,["refresh_token"]=config.RefreshToken,["grant_type"]="refresh_token"}),ct);
        if(!tokenResponse.IsSuccessStatusCode) throw new ApiException(503,"CALENDAR_AUTH_UNAVAILABLE");
        using var token=JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(ct));
        using var message=new HttpRequestMessage(method,url);
        message.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token.RootElement.GetProperty("access_token").GetString());
        if(body!=null) message.Content=JsonContent.Create(body);
        return await http.SendAsync(message,ct);
    }
    private CalendarEvent Parse(JsonElement e) {
        DateTimeOffset Endpoint(JsonElement p) {
            if(p.TryGetProperty("dateTime",out var dt)) return DateTimeOffset.Parse(dt.GetString()!,CultureInfo.InvariantCulture);
            return booking.At(DateOnly.ParseExact(p.GetProperty("date").GetString()!,"yyyy-MM-dd",CultureInfo.InvariantCulture),TimeOnly.MinValue);
        }
        string? fingerprint=null,id=null;
        if(e.TryGetProperty("extendedProperties",out var ex) && ex.TryGetProperty("private",out var priv)) {
            if(priv.TryGetProperty("fingerprint",out var f)) fingerprint=f.GetString();
            if(priv.TryGetProperty("bookingId",out var b)) id=b.GetString();
        }
        return new(e.GetProperty("id").GetString()!,Endpoint(e.GetProperty("start")),Endpoint(e.GetProperty("end")),fingerprint,id);
    }
    public async Task<IReadOnlyList<CalendarEvent>> ListAsync(DateTimeOffset from,DateTimeOffset to,CancellationToken ct) {
        var events=new List<CalendarEvent>(); string? page=null;
        do {
            var url=Root+"?singleEvents=true&showDeleted=false&maxResults=2500&timeMin="+Uri.EscapeDataString(from.ToString("O"))+"&timeMax="+Uri.EscapeDataString(to.ToString("O"))+"&timeZone="+Uri.EscapeDataString(booking.TimeZone);
            if(page!=null) url+="&pageToken="+Uri.EscapeDataString(page);
            using var response=await SendAsync(HttpMethod.Get,url,null,ct);
            if(!response.IsSuccessStatusCode) throw new ApiException(503,"CALENDAR_UNAVAILABLE");
            using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            foreach(var e in doc.RootElement.GetProperty("items").EnumerateArray()) {
                if(e.TryGetProperty("status",out var status) && status.GetString()=="cancelled") continue;
                // All existing events block time, including manually-created/all-day events.
                events.Add(Parse(e));
            }
            page=doc.RootElement.TryGetProperty("nextPageToken",out var next)?next.GetString():null;
        } while(page!=null);
        return events;
    }
    public async Task<CalendarEvent?> FindAsync(string eventId,CancellationToken ct) {
        using var response=await SendAsync(HttpMethod.Get,Root+"/"+eventId,null,ct);
        if(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone) return null;
        if(!response.IsSuccessStatusCode) throw new ApiException(503,"CALENDAR_UNAVAILABLE");
        using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if(doc.RootElement.TryGetProperty("status",out var status) && status.GetString()=="cancelled") throw new ApiException(409,"BOOKING_CANCELLED");
        return Parse(doc.RootElement);
    }
    public async Task<CalendarEvent> CreateAsync(string eventId,string bookingId,string fingerprint,BookingRequest request,DateTimeOffset start,DateTimeOffset end,CancellationToken ct) {
        var payload=new { id=eventId,summary="Prenotazione - "+request.Customer.Name,
            description=$"Prenotazione dal sito\n\nBooking ID: {bookingId}\nCliente: {WebUtility.HtmlEncode(request.Customer.Name)}\nEmail: {WebUtility.HtmlEncode(request.Customer.Email)}\nTelefono: {WebUtility.HtmlEncode(request.Customer.Phone)}",
            start=new { dateTime=start.ToString("O"),timeZone=booking.TimeZone },end=new { dateTime=end.ToString("O"),timeZone=booking.TimeZone },
            extendedProperties=new { @private=new { bookingId,fingerprint } } };
        using var response=await SendAsync(HttpMethod.Post,Root+"?sendUpdates=none",payload,ct);
        if(response.StatusCode==HttpStatusCode.Conflict) {
            var existing=await FindAsync(eventId,ct);
            if(existing?.Fingerprint==fingerprint) return existing;
            throw new ApiException(409,"IDEMPOTENCY_CONFLICT");
        }
        if(!response.IsSuccessStatusCode) throw new ApiException(503,"CALENDAR_WRITE_UNAVAILABLE");
        using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return Parse(doc.RootElement);
    }
}

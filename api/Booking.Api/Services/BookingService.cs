using System.Globalization;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using Booking.Api.Configuration;
using Booking.Api.Models;
using Booking.Api.Utils;
namespace Booking.Api.Services;
public sealed class BookingService(BookingOptions options,AvailabilityService availability,ICalendarService calendar,IEmailService email,ICaptchaService captcha,ILogger<BookingService> logger)
{
    public static (BookingRequest Request,DateTimeOffset Start) Validate(BookingRequest request) {
        if(request is null || request.Customer is null || request.Start is null || request.Start.Length>40 || !Regex.IsMatch(request.Start,@"T.*(?:Z|[+-]\d{2}:\d{2})$") || !DateTimeOffset.TryParse(request.Start,CultureInfo.InvariantCulture,DateTimeStyles.None,out var start)) throw new ApiException(400,"INVALID_REQUEST");
        string Clean(string? value,int max,bool required) {
            var text=(value??"").Trim();
            if((required && text.Length==0) || text.Length>max || text.Any(char.IsControl)) throw new ApiException(400,"INVALID_CUSTOMER");
            return text;
        }
        var name=Clean(request.Customer.Name,120,true);
        var address=Clean(request.Customer.Email,254,true);
        if(!MailAddress.TryCreate(address,out var parsed) || parsed.Address!=address || !address.Contains('@') || address.Contains(' ')) throw new ApiException(400,"INVALID_EMAIL");
        var phone=Clean(request.Customer.Phone,30,false);
        if(phone.Length>0 && !Regex.IsMatch(phone,@"^\+?[0-9 ()-]{5,30}$")) throw new ApiException(400,"INVALID_PHONE");
        return (request with { Customer=new(name,address,phone) },start);
    }
    public async Task<BookingResponse> BookAsync(BookingRequest input,string? key,CancellationToken ct) {
        var (request,start)=Validate(input);
        if(!Guid.TryParseExact(key,"D",out var keyId)) throw new ApiException(400,"INVALID_IDEMPOTENCY_KEY");
        var day=DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(start,options.Zone).DateTime);
        if(day<options.StartDate || day>options.EndDate) throw new ApiException(400,"INVALID_SLOT");
        var fingerprint=BookingIdGenerator.Hash(JsonSerializer.Serialize(new { Start=start.ToUniversalTime(),request.Customer }));
        var eventId="b"+BookingIdGenerator.Hash(keyId.ToString("D"));
        // Complete an in-flight write even if the client disconnects; retries recover it by event ID.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var work = timeout.Token;
        var existing = await calendar.FindAsync(eventId, work);
        if (existing != null)
        {
            if (existing.Fingerprint != fingerprint || existing.BookingId == null)
                throw new ApiException(409, "IDEMPOTENCY_CONFLICT");
            return new(true, existing.BookingId, existing.Start, existing.End);
        }
        if (!availability.IsValid(start)) throw new ApiException(400, "INVALID_SLOT");
        await captcha.ValidateAsync(request.CaptchaToken, work);
        // Best-effort final check. Simultaneous requests with different keys may both book;
        // this is an accepted tradeoff for the low-volume flow, handled manually in Calendar.
        var events = await calendar.ListAsync(start.AddMinutes(-options.BreakMinutes),
            start.AddMinutes(options.DurationMinutes + options.BreakMinutes), work);
        if (!availability.IsFree(start, events)) throw new ApiException(409, "SLOT_NOT_AVAILABLE");
        var created = await calendar.CreateAsync(eventId, BookingIdGenerator.NewId(), fingerprint,
            request, start, start.AddMinutes(options.DurationMinutes), work);
        var result = new BookingResponse(true, created.BookingId!, created.Start, created.End);
        // No rollback after a confirmed Calendar write, including email failure.
        try { using var emailTimeout=new CancellationTokenSource(TimeSpan.FromSeconds(20)); result=result with { EmailSent=await email.SendAsync(request,result,emailTimeout.Token) }; }
        catch(Exception) { logger.LogWarning("Email unavailable after Calendar confirmation."); }
        return result;
    }
}

using Azure;
using Azure.Communication.Email;
using Booking.Api.Configuration;
using Booking.Api.Models;
using System.Globalization;
namespace Booking.Api.Services;
public sealed class EmailService(EmailOptions options,BookingOptions rules,ILogger<EmailService> logger) : IEmailService {
    public async Task<bool> SendAsync(BookingRequest request,BookingResponse booking,CancellationToken ct) {
        if(string.IsNullOrWhiteSpace(options.ConnectionString) || string.IsNullOrWhiteSpace(options.Sender)) { logger.LogWarning("Email not configured; booking is confirmed."); return false; }
        try {
            var local=TimeZoneInfo.ConvertTime(booking.Start,rules.Zone);
            var content=new EmailContent("Prenotazione confermata") { PlainText=$"Ciao {request.Customer.Name},\n\nla tua prenotazione è confermata.\n\nData: {local.ToString("d MMMM yyyy",CultureInfo.GetCultureInfo("it-IT"))}\nOrario: {local:HH:mm}\nDurata: {rules.DurationMinutes} minuti\nCodice: {booking.BookingId}\n\nPer modifiche alla prenotazione contatta direttamente la struttura." };
            var operation=await new EmailClient(options.ConnectionString).SendAsync(WaitUntil.Completed,new EmailMessage(options.Sender,request.Customer.Email,content),ct);
            return operation.Value.Status==EmailSendStatus.Succeeded;
        } catch(Exception) { logger.LogWarning("Confirmation email failed; booking remains confirmed."); return false; }
    }
}

using Booking.Api.Configuration;
using Booking.Api.Models;
using MimeKit;
using System.Globalization;
namespace Booking.Api.Services;

public sealed class EmailService(EmailOptions options, BookingOptions rules, ISmtpEmailTransport transport,
    ILogger<EmailService> logger) : IEmailService
{
    public Task<bool> SendAsync(BookingRequest request, BookingResponse booking, CancellationToken ct)
    {
        var local = TimeZoneInfo.ConvertTime(booking.Start, rules.Zone);
        var text = $"Ciao {request.Customer.Name},\n\nla tua prenotazione è confermata.\n\nData: {local.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("it-IT"))}\nOrario: {local:HH:mm}\nDurata: {rules.DurationMinutes} minuti\nCodice: {booking.BookingId}\n\nPer modifiche alla prenotazione contatta direttamente la struttura.";
        return SendMessageAsync(request.Customer.Email, "Prenotazione confermata", text, ct);
    }

    public Task<bool> SendTestAsync(string recipient, CancellationToken ct) => SendMessageAsync(recipient,
        "Test email prenotazioni",
        "Questa è un'email di prova del sito Appuntamenti di Natale 2026, inviata tramite Gmail SMTP.\n\nNon è stata creata alcuna prenotazione né alcun evento nel calendario.", ct);

    private async Task<bool> SendMessageAsync(string recipient, string subject, string text, CancellationToken ct)
    {
        if (!options.Configured)
        {
            logger.LogWarning("Gmail email not configured; set GMAIL_ADDRESS and GMAIL_APP_PASSWORD.");
            return false;
        }

        try
        {
            using var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(options.Sender));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = text };
            await transport.SendAsync(message, options.Sender, options.AppPassword, ct);
            // Success means Gmail accepted the message, not that it reached the recipient's inbox.
            return true;
        }
        catch (MailKit.Security.AuthenticationException)
        {
            logger.LogWarning("Gmail authentication failed. Check the sender address and Google app password.");
            return false;
        }
        catch (MailKit.Net.Smtp.SmtpCommandException ex)
        {
            logger.LogWarning("Gmail rejected the SMTP command (status {StatusCode}, error {ErrorCode}). Booking remains confirmed.", ex.StatusCode, ex.ErrorCode);
            return false;
        }
        catch (Exception ex)
        {
            // Provider exceptions can contain addresses or credentials; never log them.
            logger.LogWarning("Gmail email failed ({ErrorType}); check configuration and account access. Booking remains confirmed.", ex.GetType().Name);
            return false;
        }
    }
}

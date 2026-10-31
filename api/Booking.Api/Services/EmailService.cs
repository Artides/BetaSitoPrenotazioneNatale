using Booking.Api.Configuration;
using Booking.Api.Models;
using MimeKit;
using System.Globalization;
using System.Net;
namespace Booking.Api.Services;

public sealed class EmailService(EmailOptions options, BookingOptions rules, ISmtpEmailTransport transport,
    ILogger<EmailService> logger) : IEmailService
{
    public Task<bool> SendAsync(BookingRequest request, BookingResponse booking, CancellationToken ct)
    {
        var local = TimeZoneInfo.ConvertTime(booking.Start, rules.Zone);
        var date = local.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("it-IT"));
        var time = local.ToString("HH:mm", CultureInfo.InvariantCulture);
        var text = $"Ciao {request.Customer.Name},\n\nla tua prenotazione è confermata! Ti aspetto per il tuo momento di Natale.\n\nData: {date}\nOrario: {time}\nDurata: {rules.DurationMinutes} minuti\nCodice: {booking.BookingId}\n\nPer modifiche alla prenotazione contattami direttamente su WhatsApp al numero 3926404457.\nhttps://wa.me/393926404457\n\nA presto!\nAppuntamenti di Natale 2026";
        var name = WebUtility.HtmlEncode(request.Customer.Name);
        var code = WebUtility.HtmlEncode(booking.BookingId);
        var html = $$"""
            <!doctype html>
            <html lang="it">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Prenotazione confermata</title></head>
            <body style="margin:0;padding:0;background-color:#f5f5f3;color:#292a29;font-family:Arial,Helvetica,sans-serif;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color:#f5f5f3;">
                <tr><td align="center" style="padding:24px 12px;">
                  <table role="presentation" width="560" cellspacing="0" cellpadding="0" border="0" style="width:100%;max-width:560px;background-color:#ffffff;">
                    <tr><td style="padding:32px 24px 24px;">
                      <h1 style="margin:0 0 24px;font-family:Georgia,'Times New Roman',serif;font-size:30px;line-height:1.2;font-weight:normal;color:#9e1032;">Il tuo momento di Natale<br>è confermato.</h1>
                      <p style="margin:0 0 12px;font-size:17px;line-height:1.6;">Ciao {{name}},</p>
                      <p style="margin:0;font-size:16px;line-height:1.6;">la tua prenotazione è confermata! Ecco tutti i dettagli del nostro appuntamento.</p>
                    </td></tr>
                    <tr><td style="padding:0 24px;">
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="width:100%;border-top:1px solid #e2e3e5;border-bottom:1px solid #e2e3e5;">
                        <tr><td width="28%" style="padding:18px 12px 8px 0;vertical-align:top;font-size:14px;line-height:1.5;color:#646666;">Data</td><td style="padding:18px 0 8px;font-size:17px;line-height:1.5;font-weight:bold;">{{date}}</td></tr>
                        <tr><td style="padding:0 12px 8px 0;vertical-align:top;font-size:14px;line-height:1.5;color:#646666;">Orario</td><td style="padding:0 0 8px;font-size:17px;line-height:1.5;font-weight:bold;">{{time}}</td></tr>
                        <tr><td style="padding:0 12px 18px 0;vertical-align:top;font-size:14px;line-height:1.5;color:#646666;">Durata</td><td style="padding:0 0 18px;font-size:16px;line-height:1.5;">{{rules.DurationMinutes}} minuti</td></tr>
                      </table>
                      <p style="margin:14px 0 0;font-size:12px;line-height:1.6;color:#646666;overflow-wrap:anywhere;word-break:break-word;">Codice prenotazione: {{code}}</p>
                    </td></tr>
                    <tr><td style="padding:24px;">
                      <p style="margin:0 0 18px;font-size:15px;line-height:1.6;">Per modifiche alla prenotazione contattami direttamente su WhatsApp al numero <a href="https://wa.me/393926404457" style="color:#147445;text-decoration:underline;">3926404457</a>.</p>
                      <table role="presentation" cellspacing="0" cellpadding="0" border="0"><tr><td align="center" bgcolor="#147445" style="background-color:#147445;border-radius:8px;mso-padding-alt:14px 20px;">
                        <a href="https://wa.me/393926404457" style="display:inline-block;padding:14px 20px;color:#ffffff;font-size:15px;line-height:1.4;font-weight:bold;text-decoration:none;">Scrivimi su WhatsApp</a>
                      </td></tr></table>
                      <p style="margin:28px 0 0;font-size:16px;line-height:1.6;">A presto!</p>
                      <p style="margin:4px 0 0;font-size:13px;line-height:1.6;color:#646666;">Appuntamenti di Natale 2026</p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
        return SendMessageAsync(request.Customer.Email, "Prenotazione confermata", text, ct, html);
    }

    public Task<bool> SendTestAsync(string recipient, CancellationToken ct) => SendMessageAsync(recipient,
        "Test email prenotazioni",
        "Questa è un'email di prova del sito Appuntamenti di Natale 2026, inviata tramite Gmail SMTP.\n\nNon è stata creata alcuna prenotazione né alcun evento nel calendario.", ct);

    private async Task<bool> SendMessageAsync(string recipient, string subject, string text, CancellationToken ct, string? html = null)
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
            message.Body = new BodyBuilder { TextBody = text, HtmlBody = html }.ToMessageBody();
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

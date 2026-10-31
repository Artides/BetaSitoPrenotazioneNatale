using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net.Security;
namespace Booking.Api.Services;

public interface ISmtpEmailTransport
{
    Task SendAsync(MimeMessage message, string username, string appPassword, CancellationToken ct);
}

public sealed class GmailSmtpTransport(ILogger<GmailSmtpTransport> logger) : ISmtpEmailTransport
{
    public async Task SendAsync(MimeMessage message, string username, string appPassword, CancellationToken ct)
    {
        // Each send owns its connection; no SMTP state is shared between concurrent bookings.
        using var client = new SmtpClient { Timeout = 15000 };
        client.ServerCertificateValidationCallback = (_, _, chain, errors) =>
        {
            if (errors != SslPolicyErrors.None)
                logger.LogWarning("Gmail TLS certificate rejected ({PolicyErrors}; chain: {ChainStatus}).",
                    errors, chain == null ? "unavailable" : string.Join(", ", chain.ChainStatus.Select(s => s.Status)));
            // Retain strict chain and hostname validation; never accept an invalid certificate.
            return errors == SslPolicyErrors.None;
        };
        // Require STARTTLS and normal certificate validation before sending any credentials.
        await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, ct);
        client.AuthenticationMechanisms.Remove("XOAUTH2");
        await client.AuthenticateAsync(username, appPassword, ct);
        await client.SendAsync(message, ct);
        // Delivery was accepted. A QUIT failure must not turn a successful send into a failure.
        try { await client.DisconnectAsync(true, ct); }
        catch (Exception) { }
    }
}

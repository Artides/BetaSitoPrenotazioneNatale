using Booking.Api.Configuration;
using Booking.Api.Models;
using Booking.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using Xunit;
namespace Booking.Api.Tests;

public sealed class EmailTests
{
    private static readonly EmailOptions Options = new("sender@gmail.com", "abcdefghijklmnop");
    private static readonly BookingRequest Request = new("2026-11-10T09:00:00Z", new("Mario Rossi", "customer@example.com"));
    private static readonly BookingResponse Booking = new(true, "booking-code",
        DateTimeOffset.Parse("2026-11-10T09:00:00Z"), DateTimeOffset.Parse("2026-11-10T09:30:00Z"));

    [Fact]
    public void LoadsDedicatedGmailSecretsAndRemovesPasswordDisplaySpaces()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GMAIL_ADDRESS"] = " sender@gmail.com ",
            ["GMAIL_APP_PASSWORD"] = "abcd efgh ijkl mnop",
            ["EMAIL_SENDER"] = "old@example.com",
            ["EMAIL_CONNECTION_STRING"] = "old-connection"
        }).Build();
        var options = EmailOptions.Load(config);
        Assert.True(options.Configured);
        Assert.Equal("sender@gmail.com", options.Sender);
        Assert.Equal("abcdefghijklmnop", options.AppPassword);
        Assert.DoesNotContain(options.AppPassword, options.ToString()!);
    }

    [Theory]
    [InlineData("", "abcdefghijklmnop")]
    [InlineData("sender@gmail.com", "")]
    [InlineData("sender@gmail.com", " ")]
    public async Task MissingConfigurationNeverCallsSmtp(string sender, string password)
    {
        var transport = new FakeTransport();
        var service = Service(new(sender, password), transport);
        Assert.False(await service.SendAsync(Request, Booking, default));
        Assert.Equal(0, transport.Sends);
    }

    [Fact]
    public async Task ConfirmationUsesGmailSenderAndItalianLocalAppointmentTime()
    {
        var transport = new FakeTransport();
        Assert.True(await Service(Options, transport).SendAsync(Request, Booking, default));
        Assert.Equal("sender@gmail.com", transport.From);
        Assert.Equal("customer@example.com", transport.To);
        Assert.Equal("Prenotazione confermata", transport.Subject);
        Assert.Contains("Ciao Mario Rossi", transport.Text);
        Assert.Contains("Data: 10 novembre 2026", transport.Text);
        Assert.Contains("Orario: 10:00", transport.Text);
        Assert.Contains("Durata: 30 minuti", transport.Text);
        Assert.Contains("Codice: booking-code", transport.Text);
        Assert.Equal(Options.Sender, transport.Username);
        Assert.Equal(Options.AppPassword, transport.Password);
        Assert.Equal(1, transport.Sends);
    }

    [Fact]
    public async Task SmtpFailureReturnsFalseWithoutLoggingProviderSecrets()
    {
        var transport = new FakeTransport { Error = new InvalidOperationException("secret-provider-details") };
        var logger = new CapturingLogger();
        var service = new EmailService(Options, new(), transport, logger);
        Assert.False(await service.SendAsync(Request, Booking, default));
        Assert.Single(logger.Messages);
        Assert.DoesNotContain("secret-provider-details", logger.Messages[0]);
        Assert.DoesNotContain(Options.AppPassword, logger.Messages[0]);
    }

    [Fact]
    public async Task CancellationReachesTransportAndReturnsFalse()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.False(await Service(Options, new FakeTransport()).SendAsync(Request, Booking, cancellation.Token));
    }

    [Fact]
    public async Task TestEmailIsClearlyMarkedAndUsesRequestedRecipient()
    {
        var transport = new FakeTransport();
        Assert.True(await Service(Options, transport).SendTestAsync("test@example.com", default));
        Assert.Equal("test@example.com", transport.To);
        Assert.Equal("Test email prenotazioni", transport.Subject);
        Assert.Contains("Non è stata creata alcuna prenotazione", transport.Text);
        Assert.DoesNotContain("booking-code", transport.Text);
    }

    [Fact]
    public async Task BookingRetryDoesNotSendSecondGmailConfirmation()
    {
        var rules = new BookingOptions();
        var calendar = new FakeCalendar();
        var transport = new FakeTransport();
        var service = new BookingService(rules, new(rules, calendar, new FixedClock()), calendar,
            Service(Options, transport), new NoCaptcha(), NullLogger<BookingService>.Instance);
        var key = Guid.NewGuid().ToString();
        var first = await service.BookAsync(Request, key, default);
        var retry = await service.BookAsync(Request, key, default);
        Assert.True(first.EmailSent);
        Assert.False(retry.EmailSent);
        Assert.Equal(first.BookingId, retry.BookingId);
        Assert.Equal(1, transport.Sends);
    }

    private static EmailService Service(EmailOptions options, FakeTransport transport) =>
        new(options, new(), transport, NullLogger<EmailService>.Instance);

    private sealed class FakeTransport : ISmtpEmailTransport
    {
        public int Sends { get; private set; }
        public Exception? Error { get; init; }
        public string? From, To, Subject, Text, Username, Password;

        public Task SendAsync(MimeMessage message, string username, string appPassword, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Sends++;
            if (Error != null) throw Error;
            From = message.From.Mailboxes.Single().Address;
            To = message.To.Mailboxes.Single().Address;
            Subject = message.Subject;
            Text = message.TextBody;
            Username = username;
            Password = appPassword;
            return Task.CompletedTask;
        }
    }

    private sealed class NoCaptcha : ICaptchaService
    {
        public Task ValidateAsync(string? token, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class CapturingLogger : ILogger<EmailService>
    {
        public List<string> Messages { get; } = [];
        public bool IsEnabled(LogLevel level) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            Assert.Null(exception);
        }
    }
}

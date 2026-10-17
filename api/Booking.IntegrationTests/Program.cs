using System.Globalization;
using Booking.Api.Configuration;
using Booking.Api.Models;
using Booking.Api.Services;
using Booking.Api.Utils;
using Microsoft.Extensions.Configuration;

// Explicit live opt-in; no secret values, provider response bodies, or customer data are printed.
if (args.Length != 3 || args[0] != "--create" ||
    !DateOnly.TryParseExact(args[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) ||
    !TimeOnly.TryParseExact(args[2], "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
{
    Console.Error.WriteLine("Uso: dotnet run --project api/Booking.IntegrationTests -- --create yyyy-MM-dd HH:mm");
    return 2;
}

try
{
    // Standard .NET configuration loads secrets internally. Never enumerate or print configuration.
    var config = new ConfigurationBuilder().AddEnvironmentVariables()
        .AddUserSecrets<IntegrationMarker>(optional: false).Build();
    var google = GoogleCalendarOptions.Load(config);
    if (!google.Configured)
    {
        Console.Error.WriteLine("Configurazione Google incompleta: verificare le quattro chiavi GOOGLE_* nei secrets .NET.");
        return 2;
    }

    var rules = new BookingOptions(); // Production season remains unchanged; this tests the Calendar adapter directly.
    var start = rules.At(day, time);
    var end = start.AddMinutes(rules.DurationMinutes);
    var id = "b" + BookingIdGenerator.Hash($"sito-natale-live-integration-v1:{start:O}");
    var fingerprint = BookingIdGenerator.Hash($"integration:{start:O}");
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    var calendar = new GoogleCalendarService(http, google, rules);
    var existing = await calendar.FindAsync(id, timeout.Token);
    if (existing is null)
    {
        var events = await calendar.ListAsync(start.AddMinutes(-rules.BreakMinutes),
            end.AddMinutes(rules.BreakMinutes), timeout.Token);
        var availability = new AvailabilityService(rules, calendar, TimeProvider.System);
        if (!availability.IsFree(start, events))
        {
            Console.Error.WriteLine("Orario occupato o pausa minima non rispettata: nessun evento creato.");
            return 3;
        }
        var request = new BookingRequest(start.ToString("O"),
            new Customer("TEST INTEGRAZIONE", "integration@example.invalid"));
        existing = await calendar.CreateAsync(id, BookingIdGenerator.NewId(), fingerprint,
            request, start, end, timeout.Token);
        Console.WriteLine("Evento di test creato; nessuna email o invito inviato.");
    }
    else if (existing.Fingerprint != fingerprint)
    {
        Console.Error.WriteLine("Identificativo già presente con dati differenti: nessun evento creato.");
        return 3;
    }
    else
    {
        Console.WriteLine("Evento di test già presente: nessun duplicato creato.");
    }

    var verified = await calendar.FindAsync(id, timeout.Token);
    if (verified is null || verified.Start != start || verified.End != end ||
        verified.Fingerprint != fingerprint || string.IsNullOrWhiteSpace(verified.BookingId))
    {
        Console.Error.WriteLine("Verifica di rilettura non superata: controllare il calendario prima di riprovare.");
        return 4;
    }
    Console.WriteLine($"PASS: {day:yyyy-MM-dd}, {time:HH:mm}–{TimeOnly.FromDateTime(end.DateTime):HH:mm}, {rules.TimeZone}.");
    Console.WriteLine($"Booking ID: {verified.BookingId}");
    return 0;
}
catch (ApiException ex)
{
    Console.Error.WriteLine($"Test non completato: {ex.Code}.");
    return 1;
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Connessione al provider non riuscita ({ex.HttpRequestError}). Nessun dettaglio sensibile mostrato.");
    return 1;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Timeout: esito da verificare sul calendario; ripetere lo stesso comando evita duplicati.");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Test non completato ({ex.GetType().Name}). Nessun dettaglio sensibile mostrato.");
    return 1;
}

internal sealed class IntegrationMarker;

namespace Booking.Api.Configuration;
public sealed class EmailOptions(string sender, string appPassword)
{
    public string Sender { get; } = sender;
    public string AppPassword { get; } = appPassword;
    public bool Configured => !string.IsNullOrWhiteSpace(Sender) && !string.IsNullOrWhiteSpace(AppPassword);

    public static EmailOptions Load(IConfiguration c) => new(
        (c["GMAIL_ADDRESS"] ?? "").Trim(),
        // Google displays app passwords in groups separated by spaces.
        string.Concat((c["GMAIL_APP_PASSWORD"] ?? "").Where(ch => !char.IsWhiteSpace(ch))));
}

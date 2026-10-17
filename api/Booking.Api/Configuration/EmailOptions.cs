namespace Booking.Api.Configuration;
public sealed record EmailOptions(string ConnectionString, string Sender)
{
    public static EmailOptions Load(IConfiguration c) => new(c["EMAIL_CONNECTION_STRING"] ?? "", c["EMAIL_SENDER"] ?? "");
}

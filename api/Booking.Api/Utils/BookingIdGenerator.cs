using System.Security.Cryptography;
using System.Text;
namespace Booking.Api.Utils;
public static class BookingIdGenerator {
    public static string NewId() => Guid.NewGuid().ToString("N");
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

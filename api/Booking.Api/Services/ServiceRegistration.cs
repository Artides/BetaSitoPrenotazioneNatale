using Booking.Api.Configuration;
namespace Booking.Api.Services;
public static class ServiceRegistration {
    public static IServiceCollection AddBookingCore(this IServiceCollection services,IConfiguration config) {
        services.AddSingleton(BookingOptions.Load(config));
        services.AddSingleton(GoogleCalendarOptions.Load(config));
        services.AddSingleton(EmailOptions.Load(config));
        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient<ICalendarService,GoogleCalendarService>(c=>c.Timeout=TimeSpan.FromSeconds(15)).RemoveAllLoggers();
        services.AddHttpClient<ICaptchaService,CaptchaService>(c=>c.Timeout=TimeSpan.FromSeconds(10)).RemoveAllLoggers();
        services.AddSingleton<AvailabilityService>();services.AddSingleton<BookingService>();services.AddSingleton<ApiEndpoints>();
        services.AddMemoryCache();services.AddControllers();
        return services;
    }
}

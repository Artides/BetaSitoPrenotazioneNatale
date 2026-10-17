using Booking.Api.Services;
using Microsoft.Azure.Functions.Worker.Builder;
var builder=FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
// Explicit loading also works with func host start; real values never enter local.settings.json.
builder.Configuration.AddEnvironmentVariables();
if(builder.Environment.IsDevelopment()) builder.Configuration.AddUserSecrets<Program>(optional:true);
builder.Services.AddBookingCore(builder.Configuration);
builder.Services.AddSingleton<IEmailService,EmailService>();
builder.Build().Run();

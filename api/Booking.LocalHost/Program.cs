using Booking.Api.Configuration;
using Booking.Api.Services;
var demo = args.Contains("--demo");
var live = args.Contains("--live");
var builder = WebApplication.CreateBuilder(args.Where(a => a is not "--demo" and not "--live").ToArray());
builder.Configuration.AddEnvironmentVariables().AddUserSecrets<Program>(optional:true);
// Both modes are explicit and available only on the local Development host.
if (!builder.Environment.IsDevelopment() || demo == live)
    throw new InvalidOperationException("LocalHost requires Development and exactly one of --live (Google Calendar) or --demo (in memory).");
if (live && !GoogleCalendarOptions.Load(builder.Configuration).Configured)
    throw new InvalidOperationException("Google Calendar configuration is incomplete. Configure the four GOOGLE_* .NET user secrets.");
builder.WebHost.UseUrls("http://127.0.0.1:7071");
builder.Services.AddBookingCore(builder.Configuration);
if (demo) builder.Services.AddSingleton<ICalendarService,LocalCalendarService>();
builder.Services.AddSingleton<IEmailService,LocalEmailService>();
var app=builder.Build();
if (demo) app.Logger.LogWarning("DEMO: Calendar in memory, no real email; data disappears on shutdown.");
else app.Logger.LogWarning("LOCAL LIVE: real Google Calendar; confirmation emails are disabled.");
app.MapMethods("/api/availability",["GET","OPTIONS"],async (HttpContext ctx,ApiEndpoints endpoints)=> { var result=await endpoints.AvailabilityAsync(ctx.Request); await result.ExecuteResultAsync(new Microsoft.AspNetCore.Mvc.ActionContext { HttpContext=ctx,RouteData=ctx.GetRouteData(),ActionDescriptor=new() }); });
app.MapMethods("/api/book",["POST","OPTIONS"],async (HttpContext ctx,ApiEndpoints endpoints)=> { var result=await endpoints.BookAsync(ctx.Request); await result.ExecuteResultAsync(new Microsoft.AspNetCore.Mvc.ActionContext { HttpContext=ctx,RouteData=ctx.GetRouteData(),ActionDescriptor=new() }); });
app.Run();

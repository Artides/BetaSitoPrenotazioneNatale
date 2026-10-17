using System.Text;
using Booking.Api.Configuration;
using Booking.Api.Models;
using Booking.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
namespace Booking.Api.Tests;
public sealed class ApiTests {
    private ApiEndpoints Endpoints() {
        var rules=new BookingOptions();var cal=new FakeCalendar();var a=new AvailabilityService(rules,cal,new FixedClock());
        var b=new BookingService(rules,a,cal,new LocalEmailService(),new PassCaptcha(),NullLogger<BookingService>.Instance);
        return new(a,b,new ConfigurationBuilder().Build(),new MemoryCache(new MemoryCacheOptions()),NullLogger<ApiEndpoints>.Instance);
    }
    private static HttpRequest Request(string method="GET",string body="",string origin="http://localhost:8080") {
        var context=new DefaultHttpContext();var r=context.Request;r.Method=method;r.Headers.Origin=origin;r.Body=new MemoryStream(Encoding.UTF8.GetBytes(body));r.ContentType="application/json";r.Headers["Idempotency-Key"]=Guid.NewGuid().ToString();return r;
    }
    [Fact] public async Task RejectsUnknownClientControlledDuration() {
        var result=Assert.IsType<ObjectResult>(await Endpoints().BookAsync(Request("POST","""{"start":"2026-11-10T10:00:00+01:00","duration":60,"customer":{"name":"Mario","email":"m@example.com"}}""")));
        Assert.Equal(400,result.StatusCode);
    }
    [Fact] public async Task RejectsBadOrigin() {var result=Assert.IsType<ObjectResult>(await Endpoints().AvailabilityAsync(Request(origin:"https://evil.example")));Assert.Equal(403,result.StatusCode);}
    [Fact] public async Task PreflightAllowsIdempotencyHeader() {var r=Request("OPTIONS");Assert.Equal(204,Assert.IsType<StatusCodeResult>(await Endpoints().BookAsync(r)).StatusCode);Assert.Contains("Idempotency-Key",r.HttpContext.Response.Headers.AccessControlAllowHeaders.ToString());}
    [Fact] public async Task RateLimitStopsExcessPosts() {var e=Endpoints();for(var i=0;i<10;i++) await e.BookAsync(Request("POST","{}"));var r=Assert.IsType<ObjectResult>(await e.BookAsync(Request("POST","{}")));Assert.Equal(429,r.StatusCode);}
    [Fact] public async Task RejectsLargeBodyWithoutTrustingContentLength() {var r=Assert.IsType<ObjectResult>(await Endpoints().BookAsync(Request("POST",new string('x',8193))));Assert.Equal(413,r.StatusCode);}
    [Fact] public async Task InvalidDateRangeReturnsBadRequest() {var r=Request();r.QueryString=new("?from=2026-11-20&to=2026-11-10");Assert.Equal(400,Assert.IsType<ObjectResult>(await Endpoints().AvailabilityAsync(r)).StatusCode);}
    private sealed class PassCaptcha:ICaptchaService {public Task ValidateAsync(string? token,CancellationToken ct)=>Task.CompletedTask;}
}

using System.Globalization;
using System.Text.Json;
using Booking.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
namespace Booking.Api.Services;
public sealed class ApiEndpoints(AvailabilityService availability,BookingService booking,IConfiguration config,IMemoryCache cache,ILogger<ApiEndpoints> logger)
{
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive=false };
    private readonly object rateGate=new();
    public Task<IActionResult> AvailabilityAsync(HttpRequest request) => ExecuteAsync(request,false,async () => {
        DateOnly? Date(string key) { var value=request.Query[key].ToString(); if(value.Length==0) return null; if(!DateOnly.TryParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)) throw new ApiException(400,"INVALID_DATE"); return date; }
        return new OkObjectResult(await availability.GetAsync(Date("from"),Date("to"),request.HttpContext.RequestAborted));
    });
    public Task<IActionResult> BookAsync(HttpRequest request) => ExecuteAsync(request,true,async () => {
        if(request.ContentType is null || !request.ContentType.Split(';')[0].Trim().Equals("application/json",StringComparison.OrdinalIgnoreCase)) throw new ApiException(415,"JSON_REQUIRED");
        if(request.ContentLength>8192) throw new ApiException(413,"REQUEST_TOO_LARGE");
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(request.HttpContext.RequestAborted); timeout.CancelAfter(TimeSpan.FromSeconds(10));
        using var buffer=new MemoryStream(); var chunk=new byte[1024]; int read;
        while((read=await request.Body.ReadAsync(chunk,timeout.Token))>0) { if(buffer.Length+read>8192) throw new ApiException(413,"REQUEST_TOO_LARGE"); buffer.Write(chunk,0,read); }
        var input=JsonSerializer.Deserialize<BookingRequest>(buffer.ToArray(),Json) ?? throw new ApiException(400,"INVALID_REQUEST");
        var result=await booking.BookAsync(input,request.Headers["Idempotency-Key"].ToString(),request.HttpContext.RequestAborted);
        return new OkObjectResult(result);
    });
    private async Task<IActionResult> ExecuteAsync(HttpRequest request,bool write,Func<Task<IActionResult>> action) {
        var response=request.HttpContext.Response;
        response.Headers.CacheControl="no-store"; response.Headers["X-Content-Type-Options"]="nosniff";
        var origin=request.Headers.Origin.ToString();
        var allowed=(config["ALLOWED_ORIGINS"]??"http://localhost:8080,http://127.0.0.1:8080").Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
        if(origin.Length>0 && !allowed.Contains(origin,StringComparer.Ordinal)) return Error(403,"ORIGIN_NOT_ALLOWED");
        if(origin.Length>0) { response.Headers.AccessControlAllowOrigin=origin;response.Headers.Vary="Origin"; }
        if(HttpMethods.IsOptions(request.Method)) { response.Headers.AccessControlAllowMethods="GET, POST, OPTIONS"; response.Headers.AccessControlAllowHeaders="Content-Type, Idempotency-Key"; return new StatusCodeResult(204); }
        // Instance-local limiter; production should additionally limit requests at the edge.
        var identity=request.HttpContext.Connection.RemoteIpAddress?.ToString()??"unknown";
        var key=$"rate:{identity}:{write}";
        lock(rateGate) {
            var counter=cache.GetOrCreate(key,e=> { e.AbsoluteExpirationRelativeToNow=TimeSpan.FromMinutes(1); return new Counter(); })!;
            if(++counter.Count>(write?10:60)) { response.Headers.RetryAfter="60"; return Error(429,"RATE_LIMITED"); }
        }
        try { return await action(); }
        catch(ApiException ex) { return Error(ex.Status,ex.Code); }
        catch(JsonException) { return Error(400,"INVALID_JSON"); }
        catch(OperationCanceledException) { return Error(503,"REQUEST_TIMEOUT"); }
        catch(Exception ex) { logger.LogError("API failed ({ExceptionType}).",ex.GetType().Name); return Error(503,"SERVICE_UNAVAILABLE"); }
    }
    private static ObjectResult Error(int status,string code) => new(new {success=false,error=code}) {StatusCode=status};
    private sealed class Counter { public int Count; }
}

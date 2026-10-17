using System.Net.Http.Json;
using System.Text.Json;
using Booking.Api.Models;
namespace Booking.Api.Services;
public interface ICaptchaService { Task ValidateAsync(string? token,CancellationToken ct); }
public sealed class CaptchaService(HttpClient http,IConfiguration config) : ICaptchaService {
    public async Task ValidateAsync(string? token,CancellationToken ct) {
        var secret=config["TURNSTILE_SECRET_KEY"];
        if(string.IsNullOrWhiteSpace(secret)) return;
        if(string.IsNullOrWhiteSpace(token) || token.Length>2048) throw new ApiException(400,"CAPTCHA_REQUIRED");
        using var result=await http.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify",new FormUrlEncodedContent(new Dictionary<string,string>{["secret"]=secret,["response"]=token}),ct);
        if(!result.IsSuccessStatusCode) throw new ApiException(503,"CAPTCHA_UNAVAILABLE");
        var json=await result.Content.ReadFromJsonAsync<JsonElement>(ct);
        if(!json.GetProperty("success").GetBoolean() || json.GetProperty("action").GetString()!="booking" || !(config["TURNSTILE_ALLOWED_HOSTNAMES"]??"").Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Contains(json.GetProperty("hostname").GetString(),StringComparer.OrdinalIgnoreCase)) throw new ApiException(400,"CAPTCHA_INVALID");
    }
}

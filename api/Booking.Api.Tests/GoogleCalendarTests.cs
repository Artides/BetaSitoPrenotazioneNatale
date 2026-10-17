using System.Net;
using System.Text.Json;
using Booking.Api.Configuration;
using Booking.Api.Models;
using Booking.Api.Services;
using Xunit;
namespace Booking.Api.Tests;
public sealed class GoogleCalendarTests {
    [Fact] public async Task CalendarWriteContainsExactlyOneRealIntervalAndNoBuffer() {
        var handler=new StubHandler();
        var service=new GoogleCalendarService(new HttpClient(handler),new("calendar","client","secret","refresh"),new());
        var start=DateTimeOffset.Parse("2026-11-10T10:00:00+01:00");
        var result=await service.CreateAsync("b123","public-id","hash",new(start.ToString("O"),new("Mario <b>Rossi</b>","m@example.com")),start,start.AddMinutes(30),default);
        Assert.Single(handler.Writes);
        using var payload=JsonDocument.Parse(handler.Writes[0]);var json=payload.RootElement;
        Assert.Equal(TimeSpan.FromMinutes(30),DateTimeOffset.Parse(json.GetProperty("end").GetProperty("dateTime").GetString()!)-DateTimeOffset.Parse(json.GetProperty("start").GetProperty("dateTime").GetString()!));
        Assert.Equal("Europe/Rome",json.GetProperty("start").GetProperty("timeZone").GetString());
        Assert.Contains("&lt;b&gt;",json.GetProperty("description").GetString());Assert.DoesNotContain("<b>",json.GetProperty("description").GetString());
        Assert.False(json.TryGetProperty("buffer",out _));Assert.Equal(start.AddMinutes(30),result.End);
    }
    [Fact] public async Task ListsRecurringExpandedAndPagedEventsIncludingAllDay() {
        var handler=new StubHandler();
        var service=new GoogleCalendarService(new HttpClient(handler),new("calendar","client","secret","refresh"),new());
        var events=await service.ListAsync(DateTimeOffset.Parse("2026-11-10T00:00:00+01:00"),DateTimeOffset.Parse("2026-11-11T00:00:00+01:00"),default);
        Assert.Equal(2,events.Count);Assert.Equal(TimeSpan.FromDays(1),events[0].End-events[0].Start);
        Assert.Equal(TimeSpan.FromHours(1),events[1].End-events[1].Start);
        Assert.All(handler.ListUrls,url=>Assert.Contains("singleEvents=true",url));Assert.Contains("pageToken=second",handler.ListUrls[1]);
    }
    private sealed class StubHandler:HttpMessageHandler {
        public List<string> Writes {get;}=[];public List<string> ListUrls {get;}=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) {
            string body;
            if(request.RequestUri!.Host=="oauth2.googleapis.com") body="{\"access_token\":\"test-token\"}";
            else if(request.Method==HttpMethod.Post) {body=await request.Content!.ReadAsStringAsync(ct);Writes.Add(body);}
            else {
                ListUrls.Add(request.RequestUri.ToString());
                body=ListUrls.Count==1?"""{"items":[{"id":"all","start":{"date":"2026-11-10"},"end":{"date":"2026-11-11"}}],"nextPageToken":"second"}""":"""{"items":[{"id":"manual","start":{"dateTime":"2026-11-10T10:00:00+01:00"},"end":{"dateTime":"2026-11-10T11:00:00+01:00"}},{"id":"deleted","status":"cancelled"}]}""";
            }
            return new(HttpStatusCode.OK) {Content=new StringContent(body)};
        }
    }
}

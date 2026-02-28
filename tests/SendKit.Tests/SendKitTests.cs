using System.Net;
using System.Text.Json;
using Xunit;

namespace SendKit.Tests;

public class ClientTests
{
    [Fact]
    public void NewClientWithApiKey()
    {
        var client = new SendKitClient("sk_test_123");
        Assert.NotNull(client);
        Assert.NotNull(client.Emails);
    }

    [Fact]
    public void NewClientWithCustomBaseUrl()
    {
        var client = new SendKitClient("sk_test_123", baseUrl: "https://custom.api.com");
        Assert.NotNull(client);
    }

    [Fact]
    public void MissingApiKeyThrows()
    {
        Environment.SetEnvironmentVariable("SENDKIT_API_KEY", null);
        var ex = Assert.Throws<SendKitException>(() => new SendKitClient(""));
        Assert.Equal("missing_api_key", ex.Name);
    }

    [Fact]
    public void ClientFromEnvVariable()
    {
        Environment.SetEnvironmentVariable("SENDKIT_API_KEY", "sk_from_env");
        var client = new SendKitClient();
        Assert.NotNull(client);
        Environment.SetEnvironmentVariable("SENDKIT_API_KEY", null);
    }
}

public class EmailTests
{
    [Fact]
    public async Task SendEmail()
    {
        var handler = new MockHandler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/v1/emails", request.RequestUri!.AbsolutePath);
            Assert.Equal("Bearer sk_test_123", request.Headers.Authorization!.ToString());

            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            Assert.Equal("sender@example.com", json.RootElement.GetProperty("from").GetString());
            Assert.Equal("recipient@example.com", json.RootElement.GetProperty("to")[0].GetString());
            Assert.Equal("Test Email", json.RootElement.GetProperty("subject").GetString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"email-uuid-123"}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.com") };
        var client = new SendKitClient("sk_test_123", httpClient: httpClient);

        var result = await client.Emails.SendAsync(new SendEmailParams
        {
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Test Email",
            Html = "<p>Hello</p>"
        });

        Assert.Equal("email-uuid-123", result.Id);
    }

    [Fact]
    public async Task SendEmailWithOptionalFields()
    {
        var handler = new MockHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            Assert.Equal("reply@example.com", json.RootElement.GetProperty("reply_to").GetString());
            Assert.Equal("2026-03-01T10:00:00Z", json.RootElement.GetProperty("scheduled_at").GetString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"email-uuid-456"}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.com") };
        var client = new SendKitClient("sk_test_123", httpClient: httpClient);

        var result = await client.Emails.SendAsync(new SendEmailParams
        {
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Test",
            Html = "<p>Hi</p>",
            ReplyTo = "reply@example.com",
            ScheduledAt = "2026-03-01T10:00:00Z"
        });

        Assert.Equal("email-uuid-456", result.Id);
    }

    [Fact]
    public async Task SendMimeEmail()
    {
        var handler = new MockHandler(async request =>
        {
            Assert.Equal("/v1/emails/mime", request.RequestUri!.AbsolutePath);

            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            Assert.Equal("sender@example.com", json.RootElement.GetProperty("envelope_from").GetString());
            Assert.Equal("recipient@example.com", json.RootElement.GetProperty("envelope_to").GetString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"mime-uuid-789"}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.com") };
        var client = new SendKitClient("sk_test_123", httpClient: httpClient);

        var result = await client.Emails.SendMimeAsync(new SendMimeEmailParams
        {
            EnvelopeFrom = "sender@example.com",
            EnvelopeTo = "recipient@example.com",
            RawMessage = "From: sender@example.com\r\nTo: recipient@example.com\r\n\r\nHello"
        });

        Assert.Equal("mime-uuid-789", result.Id);
    }

    [Fact]
    public async Task ApiError()
    {
        var handler = new MockHandler(HttpStatusCode.UnprocessableEntity,
            """{"name":"validation_error","message":"The to field is required.","status_code":422}""");

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.com") };
        var client = new SendKitClient("sk_test_123", httpClient: httpClient);

        var ex = await Assert.ThrowsAsync<SendKitException>(() =>
            client.Emails.SendAsync(new SendEmailParams
            {
                From = "sender@example.com",
                To = [],
                Subject = "Test",
                Html = "<p>Hi</p>"
            }));

        Assert.Equal("validation_error", ex.Name);
        Assert.Equal("The to field is required.", ex.Message);
        Assert.Equal(422, ex.StatusCode);
    }
}

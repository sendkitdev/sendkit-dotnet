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
            Assert.Equal("/emails", request.RequestUri!.AbsolutePath);
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

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.dev") };
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
    public async Task SendEmailWithCreateFactory()
    {
        var handler = new MockHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            Assert.Equal("sender@example.com", json.RootElement.GetProperty("from").GetString());
            Assert.Equal("recipient@example.com", json.RootElement.GetProperty("to")[0].GetString());
            Assert.Equal("Test", json.RootElement.GetProperty("subject").GetString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"factory-uuid"}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.dev") };
        var client = new SendKitClient("sk_test_123", httpClient: httpClient);

        var p = SendEmailParams.Create("sender@example.com", "recipient@example.com", "Test");
        p.Html = "<p>Hello</p>";
        var result = await client.Emails.SendAsync(p);

        Assert.Equal("factory-uuid", result.Id);
    }

    [Fact]
    public async Task SendEmailWithDisplayName()
    {
        var handler = new MockHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            Assert.Contains("Bob", json.RootElement.GetProperty("to")[0].GetString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"display-uuid"}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.dev") };
        var client = new SendKitClient("sk_test_123", httpClient: httpClient);

        var result = await client.Emails.SendAsync(new SendEmailParams
        {
            From = "sender@example.com",
            To = ["Bob <recipient@example.com>"],
            Subject = "Test",
            Html = "<p>Hello</p>"
        });

        Assert.Equal("display-uuid", result.Id);
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

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.dev") };
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
            Assert.Equal("/emails/mime", request.RequestUri!.AbsolutePath);

            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            Assert.Equal("sender@example.com", json.RootElement.GetProperty("envelope_from").GetString());
            Assert.Equal("recipient@example.com", json.RootElement.GetProperty("envelope_to").GetString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"mime-uuid-789"}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.dev") };
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
    public async Task SendEmailWithMultipleRecipients()
    {
        var handler = new MockHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            var to = json.RootElement.GetProperty("to");
            Assert.Equal(3, to.GetArrayLength());
            Assert.Equal("alice@example.com", to[0].GetString());
            Assert.Equal("bob@example.com", to[1].GetString());
            Assert.Equal("charlie@example.com", to[2].GetString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"multi-uuid"}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.dev") };
        var client = new SendKitClient("sk_test_123", httpClient: httpClient);

        var result = await client.Emails.SendAsync(new SendEmailParams
        {
            From = "sender@example.com",
            To = ["alice@example.com", "bob@example.com", "charlie@example.com"],
            Subject = "Test",
            Html = "<p>Hello everyone</p>"
        });

        Assert.Equal("multi-uuid", result.Id);
    }

    [Fact]
    public async Task SendEmailWithAttachments()
    {
        var handler = new MockHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            var attachments = json.RootElement.GetProperty("attachments");
            Assert.Equal(2, attachments.GetArrayLength());

            Assert.Equal("report.pdf", attachments[0].GetProperty("filename").GetString());
            Assert.Equal("base64content", attachments[0].GetProperty("content").GetString());
            Assert.Equal("application/pdf", attachments[0].GetProperty("content_type").GetString());

            Assert.Equal("notes.txt", attachments[1].GetProperty("filename").GetString());
            Assert.Equal("plaintext", attachments[1].GetProperty("content").GetString());
            Assert.False(attachments[1].TryGetProperty("content_type", out _));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"attach-uuid"}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.dev") };
        var client = new SendKitClient("sk_test_123", httpClient: httpClient);

        var result = await client.Emails.SendAsync(new SendEmailParams
        {
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "With attachments",
            Html = "<p>See attached</p>",
            Attachments =
            [
                new Attachment { Filename = "report.pdf", Content = "base64content", ContentType = "application/pdf" },
                new Attachment { Filename = "notes.txt", Content = "plaintext" }
            ]
        });

        Assert.Equal("attach-uuid", result.Id);
    }

    [Fact]
    public async Task SendEmailWithTags()
    {
        var handler = new MockHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            var tags = json.RootElement.GetProperty("tags");
            Assert.Equal(2, tags.GetArrayLength());
            Assert.Equal("welcome", tags[0].GetString());
            Assert.Equal("onboarding", tags[1].GetString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"tags-uuid"}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.dev") };
        var client = new SendKitClient("sk_test_123", httpClient: httpClient);

        var result = await client.Emails.SendAsync(new SendEmailParams
        {
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Tagged email",
            Html = "<p>Hello</p>",
            Tags = ["welcome", "onboarding"]
        });

        Assert.Equal("tags-uuid", result.Id);
    }

    [Fact]
    public async Task SendEmailWithCcAndBcc()
    {
        var handler = new MockHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);

            var cc = json.RootElement.GetProperty("cc");
            Assert.Equal(2, cc.GetArrayLength());
            Assert.Equal("cc1@example.com", cc[0].GetString());
            Assert.Equal("cc2@example.com", cc[1].GetString());

            var bcc = json.RootElement.GetProperty("bcc");
            Assert.Equal(1, bcc.GetArrayLength());
            Assert.Equal("bcc@example.com", bcc[0].GetString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"ccbcc-uuid"}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.dev") };
        var client = new SendKitClient("sk_test_123", httpClient: httpClient);

        var result = await client.Emails.SendAsync(new SendEmailParams
        {
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "CC and BCC test",
            Html = "<p>Hello</p>",
            Cc = ["cc1@example.com", "cc2@example.com"],
            Bcc = ["bcc@example.com"]
        });

        Assert.Equal("ccbcc-uuid", result.Id);
    }

    [Fact]
    public async Task SendEmailWithTextField()
    {
        var handler = new MockHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            Assert.Equal("Plain text content", json.RootElement.GetProperty("text").GetString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"text-uuid"}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.dev") };
        var client = new SendKitClient("sk_test_123", httpClient: httpClient);

        var result = await client.Emails.SendAsync(new SendEmailParams
        {
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Test",
            Html = "<p>Hello</p>",
            Text = "Plain text content"
        });

        Assert.Equal("text-uuid", result.Id);
    }

    [Fact]
    public async Task SendEmailWithHeaders()
    {
        var handler = new MockHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            var headers = json.RootElement.GetProperty("headers");
            Assert.Equal("value", headers.GetProperty("X-Custom").GetString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"headers-uuid"}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.dev") };
        var client = new SendKitClient("sk_test_123", httpClient: httpClient);

        var result = await client.Emails.SendAsync(new SendEmailParams
        {
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Test",
            Html = "<p>Hello</p>",
            Headers = new Dictionary<string, string> { { "X-Custom", "value" } }
        });

        Assert.Equal("headers-uuid", result.Id);
    }

    [Fact]
    public async Task SendEmailNullFieldsOmitted()
    {
        var handler = new MockHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);

            Assert.True(json.RootElement.TryGetProperty("from", out _));
            Assert.True(json.RootElement.TryGetProperty("to", out _));
            Assert.True(json.RootElement.TryGetProperty("subject", out _));
            Assert.True(json.RootElement.TryGetProperty("html", out _));

            Assert.False(json.RootElement.TryGetProperty("text", out _));
            Assert.False(json.RootElement.TryGetProperty("cc", out _));
            Assert.False(json.RootElement.TryGetProperty("bcc", out _));
            Assert.False(json.RootElement.TryGetProperty("reply_to", out _));
            Assert.False(json.RootElement.TryGetProperty("headers", out _));
            Assert.False(json.RootElement.TryGetProperty("tags", out _));
            Assert.False(json.RootElement.TryGetProperty("scheduled_at", out _));
            Assert.False(json.RootElement.TryGetProperty("attachments", out _));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"minimal-uuid"}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.dev") };
        var client = new SendKitClient("sk_test_123", httpClient: httpClient);

        var result = await client.Emails.SendAsync(new SendEmailParams
        {
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Test",
            Html = "<p>Hello</p>"
        });

        Assert.Equal("minimal-uuid", result.Id);
    }

    [Fact]
    public async Task ApiError()
    {
        var handler = new MockHandler(HttpStatusCode.UnprocessableEntity,
            """{"name":"validation_error","message":"The to field is required.","status_code":422}""");

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendkit.dev") };
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

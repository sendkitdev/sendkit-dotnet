using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SendKit;

public class Emails
{
    private readonly SendKitClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    internal Emails(SendKitClient client)
    {
        _client = client;
    }

    public async Task<SendEmailResponse> SendAsync(SendEmailParams parameters, CancellationToken cancellationToken = default)
    {
        var url = $"{_client.BaseUrl}/emails";
        var response = await _client.HttpClient.PostAsJsonAsync(url, parameters, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions, cancellationToken);
            throw new SendKitException(
                error?.Message ?? "Unknown error",
                error?.Name ?? "application_error",
                error?.StatusCode
            );
        }

        return (await response.Content.ReadFromJsonAsync<SendEmailResponse>(JsonOptions, cancellationToken))!;
    }

    public async Task<SendMimeEmailResponse> SendMimeAsync(SendMimeEmailParams parameters, CancellationToken cancellationToken = default)
    {
        var url = $"{_client.BaseUrl}/emails/mime";
        var response = await _client.HttpClient.PostAsJsonAsync(url, parameters, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions, cancellationToken);
            throw new SendKitException(
                error?.Message ?? "Unknown error",
                error?.Name ?? "application_error",
                error?.StatusCode
            );
        }

        return (await response.Content.ReadFromJsonAsync<SendMimeEmailResponse>(JsonOptions, cancellationToken))!;
    }
}

public class SendEmailParams
{
    public required string From { get; set; }
    public required List<string> To { get; set; }
    public required string Subject { get; set; }

    public static SendEmailParams Create(string from, string to, string subject, string? replyTo = null, string? cc = null, string? bcc = null)
    {
        var p = new SendEmailParams { From = from, To = [to], Subject = subject };
        if (replyTo != null) p.ReplyTo = [replyTo];
        if (cc != null) p.Cc = [cc];
        if (bcc != null) p.Bcc = [bcc];
        return p;
    }
    public string? Html { get; set; }
    public string? Text { get; set; }
    public List<string>? Cc { get; set; }
    public List<string>? Bcc { get; set; }
    public List<string>? ReplyTo { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public List<Tag>? Tags { get; set; }
    public string? ScheduledAt { get; set; }
    public List<Attachment>? Attachments { get; set; }
}

public class Tag
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
}

public class Attachment
{
    public required string Filename { get; set; }
    public required string Content { get; set; }
    public string? ContentType { get; set; }
}

public class SendEmailResponse
{
    public required string Id { get; set; }
}

public class SendMimeEmailParams
{
    public required string EnvelopeFrom { get; set; }
    public required string EnvelopeTo { get; set; }
    public required string RawMessage { get; set; }
}

public class SendMimeEmailResponse
{
    public required string Id { get; set; }
}

internal class ErrorResponse
{
    public string? Name { get; set; }
    public string? Message { get; set; }
    public int? StatusCode { get; set; }
}

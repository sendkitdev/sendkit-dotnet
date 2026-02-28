using System.Net.Http.Headers;

namespace SendKit;

public class SendKitClient
{
    internal HttpClient HttpClient { get; }
    internal string BaseUrl { get; }

    public Emails Emails { get; }

    public SendKitClient(string? apiKey = null, string baseUrl = "https://api.sendkit.dev", HttpClient? httpClient = null)
    {
        var key = string.IsNullOrEmpty(apiKey)
            ? Environment.GetEnvironmentVariable("SENDKIT_API_KEY") ?? ""
            : apiKey;

        if (string.IsNullOrEmpty(key))
        {
            throw new SendKitException("Missing API key", "missing_api_key");
        }

        BaseUrl = baseUrl.TrimEnd('/');
        HttpClient = httpClient ?? new HttpClient();
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

        Emails = new Emails(this);
    }
}

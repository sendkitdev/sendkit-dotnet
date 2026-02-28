# SendKit .NET SDK

Official .NET SDK for the [SendKit](https://sendkit.com) email API.

## Installation

```bash
dotnet add package SendKit
```

## Usage

### Create a Client

```csharp
using SendKit;

var client = new SendKitClient("sk_your_api_key");
```

### Send an Email

```csharp
var response = await client.Emails.SendAsync(new SendEmailParams
{
    From = "you@example.com",
    To = ["recipient@example.com"],
    Subject = "Hello from SendKit",
    Html = "<h1>Welcome!</h1>"
});

Console.WriteLine(response.Id);
```

### Send a MIME Email

```csharp
var response = await client.Emails.SendMimeAsync(new SendMimeEmailParams
{
    EnvelopeFrom = "you@example.com",
    EnvelopeTo = "recipient@example.com",
    RawMessage = mimeString
});
```

### Error Handling

```csharp
try
{
    var response = await client.Emails.SendAsync(parameters);
    Console.WriteLine($"Sent: {response.Id}");
}
catch (SendKitException ex)
{
    Console.WriteLine($"API error: {ex.Name} ({ex.StatusCode}): {ex.Message}");
}
```

### Configuration

```csharp
// Read API key from SENDKIT_API_KEY environment variable
var client = new SendKitClient();

// Custom base URL
var client = new SendKitClient("sk_...", baseUrl: "https://custom.api.com");

// Custom HttpClient
var client = new SendKitClient("sk_...", httpClient: myHttpClient);
```

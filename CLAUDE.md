# SendKit .NET SDK

## Project Overview

.NET SDK for the SendKit email API. Zero external dependencies (uses built-in HttpClient and System.Text.Json).

## Architecture

```
src/SendKit/
├── SendKitClient.cs      # Main client, holds HttpClient
├── Emails.cs             # Emails service (SendAsync, SendMimeAsync) + models
└── SendKitException.cs   # Exception type
```

- `new SendKitClient("key")` creates client
- `client.Emails.SendAsync(params)` for structured emails
- `client.Emails.SendMimeAsync(params)` for MIME emails
- All methods are async, throw `SendKitException` on error
- `POST /v1/emails` for structured emails, `POST /v1/emails/mime` for raw MIME

## Testing

- Tests use xUnit with mock HttpMessageHandler
- Run tests: `dotnet test`
- Tests in `tests/SendKit.Tests/`

## Releasing

- Tags use numeric format: `1.0.0` (no `v` prefix)
- CI runs tests on .NET 8
- Pushing a tag creates GitHub Release + publishes to NuGet

## Git

- NEVER add `Co-Authored-By` lines to commit messages

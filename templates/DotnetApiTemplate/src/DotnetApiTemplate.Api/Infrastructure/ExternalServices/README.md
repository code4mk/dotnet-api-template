# External services

Put clients for third-party HTTP APIs here, one folder per service:

```text
ExternalServices/
└── Payments/
    ├── IPaymentClient.cs
    ├── PaymentClient.cs        // typed HttpClient
    ├── PaymentSettings.cs      // PAYMENTS_BASE_URL, PAYMENTS_API_KEY from .env
    └── PaymentDtos.cs          // request/response models of the external API
```

Register each client as a typed `HttpClient` in `AddInfrastructure`:

```csharp
services.AddEnvSettings<PaymentSettings>(configuration);
services.AddHttpClient<IPaymentClient, PaymentClient>((sp, client) =>
{
    var settings = sp.GetRequiredService<PaymentSettings>();
    client.BaseAddress = new Uri(settings.BaseUrl);
});
```

Rules: never expose external DTOs outside this folder; map them to your own types.
When the external service fails, throw `ExternalServiceException` (502, or 503 with `unavailable: true`)
with a safe message and the original exception as the inner exception.
Keep API keys in `.env` or real environment variables, never in `appsettings.json` or the committed `.env.*` presets.

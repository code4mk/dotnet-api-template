# External services

Put clients for third-party HTTP APIs here, one folder per service:

```text
ExternalServices/
└── Payments/
    ├── IPaymentClient.cs
    ├── PaymentClient.cs        // typed HttpClient
    ├── PaymentOptions.cs       // base URL, API key
    └── PaymentDtos.cs          // request/response models of the external API
```

Register each client as a typed `HttpClient` in `AddInfrastructure`:

```csharp
services.Configure<PaymentOptions>(configuration.GetSection("Payments"));
services.AddHttpClient<IPaymentClient, PaymentClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<PaymentOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});
```

Rules: never expose external DTOs outside this folder; map them to your own types.
Keep API keys in user secrets or environment variables, never in `appsettings.json`.

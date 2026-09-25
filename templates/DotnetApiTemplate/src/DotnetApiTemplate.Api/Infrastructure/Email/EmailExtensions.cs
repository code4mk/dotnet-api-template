using DotnetApiTemplate.Api.Common.Settings;
using DotnetApiTemplate.Api.Infrastructure.Email.Rendering;
using DotnetApiTemplate.Api.Infrastructure.Email.Sending;

namespace DotnetApiTemplate.Api.Infrastructure.Email;

public static class EmailExtensions
{
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEmailRenderer, ScribanEmailRenderer>();
        services.AddScoped<IEmailService, EmailService>();

        if (string.IsNullOrWhiteSpace(configuration["EMAIL_HOST"]))
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, MailKitEmailSender>();
        }

        return services;
    }
}

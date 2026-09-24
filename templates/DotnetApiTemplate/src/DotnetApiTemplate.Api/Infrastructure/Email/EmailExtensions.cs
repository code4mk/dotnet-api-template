using DotnetApiTemplate.Api.Common.Settings;

namespace DotnetApiTemplate.Api.Infrastructure.Email;

public static class EmailExtensions
{
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEnvSettings<EmailSettings>(configuration);

        if (string.IsNullOrWhiteSpace(configuration["EMAIL_HOST"]))
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }

        return services;
    }
}

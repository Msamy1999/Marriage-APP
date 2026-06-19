using MarriageApp.Core.Matching;
using MarriageApp.Core.Services;
using MarriageApp.Infrastructure.Data;
using MarriageApp.Infrastructure.Services;
using MarriageApp.Infrastructure.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarriageApp.Infrastructure;

/// <summary>Registers all Infrastructure services (EF, options, matching, notifications, photos).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        // Options bound from appsettings.json.
        services.Configure<MatchingWeights>(config.GetSection(MatchingWeights.SectionName));
        services.Configure<PhotoStorageOptions>(config.GetSection(PhotoStorageOptions.SectionName));
        services.Configure<EmailOptions>(config.GetSection(EmailOptions.SectionName));
        services.Configure<TwilioOptions>(config.GetSection(TwilioOptions.SectionName));

        // Core domain services.
        services.AddScoped<IMatchingService, MatchingService>();
        services.AddScoped<IPhotoStorageService, DataProtectionPhotoStorageService>();

        // Notification channels (all registered; each reports whether it's enabled).
        services.AddHttpClient();
        services.AddScoped<INotificationChannelSender, InAppNotificationChannel>();
        services.AddScoped<INotificationChannelSender, EmailNotificationChannel>();
        services.AddScoped<INotificationChannelSender, SmsNotificationChannel>();
        services.AddScoped<INotificationChannelSender, WhatsAppNotificationChannel>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}

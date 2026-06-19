using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using MarriageApp.Core.Entities;
using MarriageApp.Core.Enums;
using MarriageApp.Core.Services;
using MarriageApp.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarriageApp.Infrastructure.Services.Notifications;

/// <summary>In-app channel: writes the message to the DB inbox. Always enabled.</summary>
public class InAppNotificationChannel : INotificationChannelSender
{
    private readonly AppDbContext _db;
    public InAppNotificationChannel(AppDbContext db) => _db = db;

    public NotificationChannel Channel => NotificationChannel.InApp;
    public bool IsEnabled => true;

    public async Task<string> SendAsync(NotificationRequest request, CancellationToken ct = default)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = request.UserId,
            Channel = NotificationChannel.InApp,
            Title = request.Title,
            Message = request.Message,
            DeliveryStatus = "Sent",
            CreatedAt = DateTime.UtcNow,
            SentAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return "Sent";
    }
}

/// <summary>Email channel via SMTP. No-ops (Skipped) when disabled or unconfigured.</summary>
public class EmailNotificationChannel : INotificationChannelSender
{
    private readonly EmailOptions _opt;
    private readonly ILogger<EmailNotificationChannel> _logger;

    public EmailNotificationChannel(IOptions<EmailOptions> opt, ILogger<EmailNotificationChannel> logger)
    {
        _opt = opt.Value;
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Email;
    public bool IsEnabled => _opt.Enabled && !string.IsNullOrWhiteSpace(_opt.Host);

    public async Task<string> SendAsync(NotificationRequest request, CancellationToken ct = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(request.Email))
            return "Skipped (email not configured or no address)";

        try
        {
            using var client = new SmtpClient(_opt.Host, _opt.Port)
            {
                EnableSsl = _opt.UseSsl,
                Credentials = new NetworkCredential(_opt.Username, _opt.Password)
            };
            using var msg = new MailMessage
            {
                From = new MailAddress(_opt.FromAddress, _opt.FromName),
                Subject = request.Title,
                Body = request.Message,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8
            };
            msg.To.Add(request.Email);
            await client.SendMailAsync(msg, ct);
            return "Sent";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email send failed for user {UserId}", request.UserId);
            return "Failed";
        }
    }
}

/// <summary>
/// Base for Twilio-backed channels (SMS + WhatsApp). Posts to the Twilio Messages API
/// via HttpClient so we don't need the Twilio SDK. No-ops (Skipped) when unconfigured.
/// </summary>
public abstract class TwilioChannelBase : INotificationChannelSender
{
    private readonly TwilioOptions _opt;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger _logger;

    protected TwilioChannelBase(TwilioOptions opt, IHttpClientFactory httpFactory, ILogger logger)
    {
        _opt = opt;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public abstract NotificationChannel Channel { get; }
    protected abstract string From { get; }
    /// <summary>Formats the recipient phone for this channel (e.g. "whatsapp:+20..." for WhatsApp).</summary>
    protected abstract string FormatTo(string phone);

    public bool IsEnabled => _opt.Enabled
        && !string.IsNullOrWhiteSpace(_opt.AccountSid)
        && !string.IsNullOrWhiteSpace(_opt.AuthToken)
        && !string.IsNullOrWhiteSpace(From);

    public async Task<string> SendAsync(NotificationRequest request, CancellationToken ct = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(request.PhoneNumber))
            return $"Skipped ({Channel} not configured or no phone)";

        try
        {
            var http = _httpFactory.CreateClient();
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{_opt.AccountSid}/Messages.json";

            var auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_opt.AccountSid}:{_opt.AuthToken}"));
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["From"] = From,
                ["To"] = FormatTo(request.PhoneNumber),
                ["Body"] = $"{request.Title}\n{request.Message}"
            });

            var resp = await http.PostAsync(url, form, ct);
            return resp.IsSuccessStatusCode ? "Sent" : $"Failed ({(int)resp.StatusCode})";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Channel} send failed for user {UserId}", Channel, request.UserId);
            return "Failed";
        }
    }
}

public class SmsNotificationChannel : TwilioChannelBase
{
    private readonly TwilioOptions _opt;
    public SmsNotificationChannel(IOptions<TwilioOptions> opt, IHttpClientFactory http, ILogger<SmsNotificationChannel> logger)
        : base(opt.Value, http, logger) => _opt = opt.Value;

    public override NotificationChannel Channel => NotificationChannel.Sms;
    protected override string From => _opt.SmsFrom;
    protected override string FormatTo(string phone) => phone;
}

public class WhatsAppNotificationChannel : TwilioChannelBase
{
    private readonly TwilioOptions _opt;
    public WhatsAppNotificationChannel(IOptions<TwilioOptions> opt, IHttpClientFactory http, ILogger<WhatsAppNotificationChannel> logger)
        : base(opt.Value, http, logger) => _opt = opt.Value;

    public override NotificationChannel Channel => NotificationChannel.WhatsApp;
    protected override string From => _opt.WhatsAppFrom;
    protected override string FormatTo(string phone) => phone.StartsWith("whatsapp:") ? phone : $"whatsapp:{phone}";
}

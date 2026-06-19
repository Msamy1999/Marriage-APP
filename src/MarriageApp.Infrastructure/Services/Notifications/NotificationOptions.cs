namespace MarriageApp.Infrastructure.Services.Notifications;

/// <summary>SMTP settings for the email channel ("Notifications:Email" section).</summary>
public class EmailOptions
{
    public const string SectionName = "Notifications:Email";
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "no-reply@marriageapp.local";
    public string FromName { get; set; } = "منصة التعارف للزواج";
}

/// <summary>Twilio settings shared by the SMS and WhatsApp channels ("Notifications:Twilio").</summary>
public class TwilioOptions
{
    public const string SectionName = "Notifications:Twilio";
    public bool Enabled { get; set; }
    public string AccountSid { get; set; } = "";
    public string AuthToken { get; set; } = "";
    public string SmsFrom { get; set; } = "";          // e.g. +1xxxxxxxxxx
    public string WhatsAppFrom { get; set; } = "";      // e.g. whatsapp:+1xxxxxxxxxx
}

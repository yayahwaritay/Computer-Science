namespace CompSci.Core.Configuration;

public class EmailSettings
{
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "CompSci Portal";
    public SendGridSettings SendGrid { get; set; } = new();
    public MailgunSettings Mailgun { get; set; } = new();
}

public class SendGridSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseApiUrl { get; set; } = "https://api.sendgrid.com/v3/mail/send";
}

public class MailgunSettings
{
    public string ApiKey { get; set; } = string.Empty;
    /// <summary>The sending domain configured in Mailgun, e.g. "mg.compsci-portal.com".</summary>
    public string Domain { get; set; } = string.Empty;
    /// <summary>US region by default; use "https://api.eu.mailgun.net/v3" for an EU-region domain.</summary>
    public string BaseApiUrl { get; set; } = "https://api.mailgun.net/v3";
}

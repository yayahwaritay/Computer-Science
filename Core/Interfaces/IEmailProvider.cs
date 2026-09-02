namespace CompSci.Core.Interfaces;

/// <summary>
/// A single email delivery provider (e.g. SendGrid, Mailgun). Unlike <see cref="IEmailSender"/>,
/// this reports success/failure so a caller (see FallbackEmailSender) can decide whether to
/// fall back to another provider.
/// </summary>
public interface IEmailProvider
{
    string Name { get; }

    Task<bool> TrySendEmailAsync(string toEmail, string toName, string subject, string htmlBody);
}

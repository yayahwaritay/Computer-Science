using CompSci.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CompSci.Infrastructure.Email;

/// <summary>
/// Sends email via an ordered list of providers, trying each in turn until one succeeds.
/// Configured as SendGrid first, Mailgun as fallback. Never throws — email delivery must
/// never block the calling workflow (registration, approval, etc.).
/// </summary>
public class FallbackEmailSender : IEmailSender
{
    private readonly IReadOnlyList<IEmailProvider> _providers;
    private readonly ILogger<FallbackEmailSender> _logger;

    public FallbackEmailSender(SendGridEmailSender sendGrid, MailgunEmailSender mailgun, ILogger<FallbackEmailSender> logger)
    {
        _providers = new IEmailProvider[] { sendGrid, mailgun };
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        foreach (var provider in _providers)
        {
            var sent = await provider.TrySendEmailAsync(toEmail, toName, subject, htmlBody);
            if (sent)
            {
                return;
            }

            _logger.LogWarning("{Provider} failed to send email to {ToEmail}; trying next provider.", provider.Name, toEmail);
        }

        _logger.LogError("All email providers failed to send email to {ToEmail}.", toEmail);
    }
}

using System.Net.Http.Headers;
using System.Text;
using CompSci.Core.Configuration;
using CompSci.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CompSci.Infrastructure.Email;

public class MailgunEmailSender : IEmailProvider
{
    private readonly HttpClient _httpClient;
    private readonly EmailSettings _settings;
    private readonly ILogger<MailgunEmailSender> _logger;

    public MailgunEmailSender(HttpClient httpClient, IOptions<EmailSettings> settings, ILogger<MailgunEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public string Name => "Mailgun";

    public async Task<bool> TrySendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_settings.Mailgun.ApiKey) || string.IsNullOrWhiteSpace(_settings.Mailgun.Domain))
        {
            _logger.LogWarning(
                "Email not sent to {ToEmail}: EmailSettings:Mailgun:ApiKey or Domain is not configured.", toEmail);
            return false;
        }

        try
        {
            var requestUrl = $"{_settings.Mailgun.BaseApiUrl.TrimEnd('/')}/{_settings.Mailgun.Domain}/messages";

            var formValues = new Dictionary<string, string>
            {
                ["from"] = $"{_settings.SenderName} <{_settings.SenderEmail}>",
                ["to"] = $"{toName} <{toEmail}>",
                ["subject"] = subject,
                ["html"] = htmlBody
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new FormUrlEncodedContent(formValues)
            };

            var authBytes = Encoding.ASCII.GetBytes($"api:{_settings.Mailgun.ApiKey}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Failed to send email to {ToEmail} via Mailgun. Status: {StatusCode}. Response: {Body}",
                toEmail, response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {ToEmail} via Mailgun.", toEmail);
            return false;
        }
    }
}

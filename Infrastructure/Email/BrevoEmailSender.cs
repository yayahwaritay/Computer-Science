using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CompSci.Core.Configuration;
using CompSci.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CompSci.Infrastructure.Email;

public class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly EmailSettings _settings;
    private readonly ILogger<BrevoEmailSender> _logger;

    public BrevoEmailSender(HttpClient httpClient, IOptions<EmailSettings> settings, ILogger<BrevoEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("Email not sent to {ToEmail}: EmailSettings:ApiKey is not configured.", toEmail);
            return;
        }

        try
        {
            var payload = new BrevoEmailRequest
            {
                Sender = new BrevoContact { Email = _settings.SenderEmail, Name = _settings.SenderName },
                To = new List<BrevoContact> { new() { Email = toEmail, Name = toName } },
                Subject = subject,
                HtmlContent = htmlBody
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _settings.BaseApiUrl)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("api-key", _settings.ApiKey);
            request.Headers.Add("accept", "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Failed to send email to {ToEmail} via Brevo. Status: {StatusCode}. Response: {Body}",
                    toEmail, response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            // Email delivery must never block the calling workflow (registration, approval, etc.).
            _logger.LogError(ex, "Unexpected error sending email to {ToEmail} via Brevo.", toEmail);
        }
    }

    private class BrevoEmailRequest
    {
        [JsonPropertyName("sender")]
        public BrevoContact Sender { get; set; } = new();

        [JsonPropertyName("to")]
        public List<BrevoContact> To { get; set; } = new();

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("htmlContent")]
        public string HtmlContent { get; set; } = string.Empty;
    }

    private class BrevoContact
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}

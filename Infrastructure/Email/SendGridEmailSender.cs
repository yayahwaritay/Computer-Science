using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CompSci.Core.Configuration;
using CompSci.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CompSci.Infrastructure.Email;

public class SendGridEmailSender : IEmailProvider
{
    private readonly HttpClient _httpClient;
    private readonly EmailSettings _settings;
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(HttpClient httpClient, IOptions<EmailSettings> settings, ILogger<SendGridEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public string Name => "SendGrid";

    public async Task<bool> TrySendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_settings.SendGrid.ApiKey))
        {
            _logger.LogWarning("Email not sent to {ToEmail}: EmailSettings:SendGrid:ApiKey is not configured.", toEmail);
            return false;
        }

        try
        {
            var payload = new SendGridEmailRequest
            {
                Personalizations = new List<SendGridPersonalization>
                {
                    new()
                    {
                        To = new List<SendGridContact> { new() { Email = toEmail, Name = toName } }
                    }
                },
                From = new SendGridContact { Email = _settings.SenderEmail, Name = _settings.SenderName },
                Subject = subject,
                Content = new List<SendGridContent>
                {
                    new() { Type = "text/html", Value = htmlBody }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _settings.SendGrid.BaseApiUrl)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.SendGrid.ApiKey);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Failed to send email to {ToEmail} via SendGrid. Status: {StatusCode}. Response: {Body}",
                toEmail, response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {ToEmail} via SendGrid.", toEmail);
            return false;
        }
    }

    private class SendGridEmailRequest
    {
        [JsonPropertyName("personalizations")]
        public List<SendGridPersonalization> Personalizations { get; set; } = new();

        [JsonPropertyName("from")]
        public SendGridContact From { get; set; } = new();

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public List<SendGridContent> Content { get; set; } = new();
    }

    private class SendGridPersonalization
    {
        [JsonPropertyName("to")]
        public List<SendGridContact> To { get; set; } = new();
    }

    private class SendGridContact
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private class SendGridContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}

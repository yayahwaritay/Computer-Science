namespace CompSci.Core.Configuration;

public class EmailSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "CompSci Portal";
    public string BaseApiUrl { get; set; } = "https://api.brevo.com/v3/smtp/email";
}

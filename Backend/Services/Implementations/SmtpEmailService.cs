namespace Backend.Services.Implementations;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Backend.Services.Interfaces;

public class SmtpEmailService : IEmailService
{
    private const string DefaultFromAddress = "hello@mukund.xyz";
    private const string DefaultFromName = "GDG Taskboard";
    private static readonly HttpClient MailtrapHttpClient = new();

    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendProjectInvitationEmail(
        string recipientEmail,
        string projectName,
        string inviterName,
        string role,
        DateTime expiresAt)
    {
        var subject = $"You are invited to join {projectName}";

        var body = $@"Hello,

    You have been invited by {inviterName} to join the project ""{projectName}"" as {role}.

    Invitation expiry: {expiresAt:yyyy-MM-dd HH:mm} UTC

    Please sign in to your account and open the project dashboard to access this invitation.

    - GDG Taskboard";

        await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task SendTaskMentionEmail(
        string recipientEmail,
        string recipientName,
        string commenterName,
        string taskTitle,
        string commentContent)
    {
        var sanitizedPreview = string.IsNullOrWhiteSpace(commentContent)
            ? "(no content)"
            : commentContent.Trim();

        if (sanitizedPreview.Length > 280)
            sanitizedPreview = sanitizedPreview[..280] + "...";

        var subject = $"You were mentioned on task: {taskTitle}";
        var body = $@"Hello {recipientName},

    {commenterName} mentioned you in a comment on task ""{taskTitle}"".

    Comment preview:
    {sanitizedPreview}

    Open the task board to reply or take action.

    - GDG Taskboard";

        await SendEmailAsync(recipientEmail, subject, body);
    }

    private async Task SendEmailAsync(string recipientEmail, string subject, string body)
    {
        var mailtrapApiToken = GetMailtrapApiToken();

        if (!string.IsNullOrWhiteSpace(mailtrapApiToken))
        {
            await SendEmailViaMailtrapAsync(mailtrapApiToken, recipientEmail, subject, body);
            return;
        }

        var settings = GetSmtpSettings();
        await SendEmailViaSmtpAsync(settings, recipientEmail, subject, body);
    }

    private async Task SendEmailViaMailtrapAsync(string apiToken, string recipientEmail, string subject, string body)
    {
        var sender = GetSenderIdentity();
        var payload = new
        {
            from = new
            {
                email = sender.FromAddress,
                name = sender.FromName
            },
            to = new[]
            {
                new { email = recipientEmail }
            },
            subject,
            text = body,
            category = "GDG Hackathon"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://send.api.mailtrap.io/api/send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await MailtrapHttpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return;

        var errorBody = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"Mailtrap API send failed ({(int)response.StatusCode} {response.StatusCode}): {errorBody}");
    }

    private static async Task SendEmailViaSmtpAsync(SmtpSettings settings, string recipientEmail, string subject, string body)
    {

        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        message.To.Add(recipientEmail);

        using var smtpClient = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = settings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(settings.SmtpUser))
        {
            smtpClient.Credentials = new NetworkCredential(settings.SmtpUser, settings.SmtpPassword ?? string.Empty);
        }

        await smtpClient.SendMailAsync(message);
    }

    private string? GetMailtrapApiToken()
    {
        return FirstNonEmpty(
            Environment.GetEnvironmentVariable("SMTP_API_KEY"),
            Environment.GetEnvironmentVariable("SMPTAPIKEY"),
            Environment.GetEnvironmentVariable("SMTPAPIKEY"),
            Environment.GetEnvironmentVariable("smptapikey"),
            Environment.GetEnvironmentVariable("MAILTRAP_API_TOKEN"),
            _configuration["Email:ApiKey"],
            _configuration["Mailtrap:ApiToken"]);
    }

    private SenderIdentity GetSenderIdentity()
    {
        var fromAddress = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SMTP_FROM_ADDRESS"),
            Environment.GetEnvironmentVariable("MAILTRAP_FROM_ADDRESS"),
            _configuration["Email:FromAddress"],
            _configuration["Mailtrap:FromAddress"],
            DefaultFromAddress) ?? DefaultFromAddress;

        var fromName = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SMTP_FROM_NAME"),
            Environment.GetEnvironmentVariable("MAILTRAP_FROM_NAME"),
            _configuration["Email:FromName"],
            _configuration["Mailtrap:FromName"],
            DefaultFromName) ?? DefaultFromName;

        return new SenderIdentity
        {
            FromAddress = fromAddress,
            FromName = fromName
        };
    }

    private SmtpSettings GetSmtpSettings()
    {
        var smtpHost = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SMTP_HOST"),
            _configuration["Email:SmtpHost"]);

        var smtpPortRaw = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SMTP_PORT"),
            _configuration["Email:SmtpPort"]);

        var smtpUser = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SMTP_USERNAME"),
            _configuration["Email:Username"]);

        var smtpPassword = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SMTP_PASSWORD"),
            _configuration["Email:Password"]);

        var sender = GetSenderIdentity();

        var useSslRaw = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SMTP_USE_SSL"),
            _configuration["Email:UseSsl"]);

        if (string.IsNullOrWhiteSpace(smtpHost))
            throw new InvalidOperationException("SMTP host is not configured. Set SMTP_HOST or configure Mailtrap token via SMTP_API_KEY.");

        if (!int.TryParse(smtpPortRaw, out var smtpPort))
            smtpPort = 587;

        var useSsl = true;
        if (!string.IsNullOrWhiteSpace(useSslRaw) && bool.TryParse(useSslRaw, out var parsedUseSsl))
            useSsl = parsedUseSsl;

        return new SmtpSettings
        {
            SmtpHost = smtpHost,
            SmtpPort = smtpPort,
            SmtpUser = smtpUser,
            SmtpPassword = smtpPassword,
            FromAddress = sender.FromAddress,
            FromName = sender.FromName,
            UseSsl = useSsl
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private sealed class SenderIdentity
    {
        public string FromAddress { get; init; } = DefaultFromAddress;
        public string FromName { get; init; } = DefaultFromName;
    }

    private sealed class SmtpSettings
    {
        public string SmtpHost { get; init; } = string.Empty;
        public int SmtpPort { get; init; }
        public string? SmtpUser { get; init; }
        public string? SmtpPassword { get; init; }
        public string FromAddress { get; init; } = string.Empty;
        public string FromName { get; init; } = "GDG Taskboard";
        public bool UseSsl { get; init; }
    }
}

using System.Net;
using System.Net.Mail;

namespace WebAPI.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var s = _config.GetSection("EmailSettings");

        var host        = s["SmtpHost"]    ?? throw new InvalidOperationException("EmailSettings:SmtpHost не задан");
        var port        = int.Parse(s["SmtpPort"] ?? "587");
        var enableSsl   = bool.Parse(s["EnableSsl"] ?? "true");
        var fromAddress = s["FromAddress"] ?? throw new InvalidOperationException("EmailSettings:FromAddress не задан");
        var fromName    = s["FromName"] ?? fromAddress;
        var username    = s["Username"]    ?? throw new InvalidOperationException("EmailSettings:Username не задан");
        var password    = s["Password"]    ?? throw new InvalidOperationException("EmailSettings:Password не задан");

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl   = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        using var message = new MailMessage
        {
            From       = new MailAddress(fromAddress, fromName),
            Subject    = subject,
            Body       = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(to);

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("Email отправлен на {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки email на {To}", to);
            throw;
        }
    }
}

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

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

        // Собираем письмо
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        // Выбираем режим TLS в зависимости от порта
        // Порт 465 → ImplicitTls (SSL сразу)
        // Порт 587 → StartTls   (STARTTLS поверх обычного соединения)
        // Порт 25  → Auto       (без шифрования / по возможности)
        var secureOption = port == 465
            ? SecureSocketOptions.SslOnConnect
            : enableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, secureOption);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);

            _logger.LogInformation("Email отправлен на {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки email на {To}: {Message}", to, ex.Message);
            throw;
        }
    }
}

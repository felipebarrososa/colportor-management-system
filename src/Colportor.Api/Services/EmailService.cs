using MailKit.Net.Smtp;
using MimeKit;
namespace Colportor.Api.Services;
public class EmailService
{
    private readonly IConfiguration _cfg;
    public EmailService(IConfiguration cfg){ _cfg = cfg; }
    public async Task SendAsync(string to, string subject, string html)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress("Colportor ID", _cfg["Smtp:From"] ?? "no-reply@colportor.local"));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        var body = new BodyBuilder{ HtmlBody = html }.ToMessageBody();
        msg.Body = body;
        using var client = new SmtpClient();
        await client.ConnectAsync(_cfg["Smtp:Host"] ?? "localhost", int.Parse(_cfg["Smtp:Port"] ?? "1025"), false);
        await client.SendAsync(msg);
        await client.DisconnectAsync(true);
    }
}

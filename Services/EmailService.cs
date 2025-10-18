using ALODAN.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace ALODAN.Helpers
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public void EnviarCorreo(string destino, string asunto, string cuerpoHtml, byte[]? adjunto = null, string? nombreAdjunto = null)
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(_emailSettings.Nombre, _emailSettings.Email));
            mensaje.To.Add(new MailboxAddress("", destino));
            mensaje.Subject = asunto;

            var builder = new BodyBuilder { HtmlBody = cuerpoHtml };

            if (adjunto != null && nombreAdjunto != null)
                builder.Attachments.Add(nombreAdjunto, adjunto);

            mensaje.Body = builder.ToMessageBody();

            using (var smtp = new SmtpClient())
            {
                smtp.Connect(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);
                smtp.Authenticate(_emailSettings.Email, _emailSettings.Password);
                smtp.Send(mensaje);
                smtp.Disconnect(true);
            }
        }
    }
}

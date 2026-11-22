using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SysPuntoVenta.Backend.Services
{
    /// <summary>
    /// Implementación del servicio de correo electrónico mediante SMTP
    /// con soporte de simulación segura en consola para entornos de desarrollo.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Envía un correo electrónico mediante el cliente SMTP configurado o lo simula en consola en desarrollo.
        /// </summary>
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var simular = _config.GetValue<bool>("EmailSettings:SimularEnDesarrollo", true);
            var host = _config["EmailSettings:Host"];

            if (simular || string.IsNullOrWhiteSpace(host))
            {
                Console.WriteLine("===================================================================");
                Console.WriteLine($"[EMAIL SIMULADO] Destino: {toEmail} | Asunto: {subject}");
                Console.WriteLine("-------------------------------------------------------------------");
                Console.WriteLine(htmlBody);
                Console.WriteLine("===================================================================");
                return await Task.FromResult(true);
            }

            try
            {
                var port = _config.GetValue<int>("EmailSettings:Port", 587);
                var enableSsl = _config.GetValue<bool>("EmailSettings:EnableSsl", true);
                var user = _config["EmailSettings:UserName"] ?? "";
                var pass = _config["EmailSettings:Password"] ?? "";
                var fromEmail = _config["EmailSettings:FromEmail"] ?? user;
                var fromName = _config["EmailSettings:FromName"] ?? "SysFacturación Seguridad";

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(user, pass)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                message.To.Add(toEmail);
                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR EMAIL] No se pudo enviar el correo a {toEmail}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Genera y envía la plantilla HTML con el código OTP de 6 dígitos para restablecer la contraseña.
        /// </summary>
        public async Task<bool> SendPasswordResetCodeAsync(string toEmail, string nombre, string codigoOtp)
        {
            var subject = $"Código de Recuperación de Contraseña: {codigoOtp}";
            var html = $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif; background-color: #0B0F19; color: #F8FAFC; padding: 25px;'>
    <div style='max-width: 520px; margin: 0 auto; background-color: #161B2E; border: 1px solid #2E2856; border-radius: 10px; padding: 25px;'>
        <h2 style='color: #8B5CF6; margin-top: 0;'>SysFacturación - Seguridad</h2>
        <p>Hola <strong>{WebUtility.HtmlEncode(nombre)}</strong>,</p>
        <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta.</p>
        <div style='text-align: center; margin: 25px 0;'>
            <span style='display: inline-block; background: linear-gradient(135deg, #2563EB, #7C3AED); color: #FFFFFF; font-size: 28px; font-weight: bold; letter-spacing: 6px; padding: 12px 24px; border-radius: 8px;'>
                {codigoOtp}
            </span>
        </div>
        <p style='color: #94A3B8; font-size: 13px;'>Este código de verificación es válido durante <strong>15 minutos</strong>. Si tú no realizaste esta solicitud, puedes ignorar este mensaje.</p>
        <hr style='border: 0; border-top: 1px solid #2E2856; margin: 20px 0;'/>
        <p style='color: #64748B; font-size: 11px; text-align: center;'>© 2026 SysPuntoVenta. Todos los derechos reservados.</p>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, html);
        }

        /// <summary>
        /// Genera y envía un correo de notificación confirmando que la contraseña fue cambiada satisfactoriamente.
        /// </summary>
        public async Task<bool> SendPasswordChangedNotificationAsync(string toEmail, string nombre, string ipAddress)
        {
            var subject = "Seguridad: Tu contraseña de SysFacturación ha sido actualizada";
            var html = $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif; background-color: #0B0F19; color: #F8FAFC; padding: 25px;'>
    <div style='max-width: 520px; margin: 0 auto; background-color: #161B2E; border: 1px solid #2E2856; border-radius: 10px; padding: 25px;'>
        <h2 style='color: #10B981; margin-top: 0;'>Contraseña Actualizada</h2>
        <p>Hola <strong>{WebUtility.HtmlEncode(nombre)}</strong>,</p>
        <p>Te confirmamos que la contraseña de tu cuenta en <strong>SysFacturación</strong> fue modificada correctamente desde la IP <code>{ipAddress}</code>.</p>
        <p style='color: #94A3B8; font-size: 13px;'>Por motivos de seguridad, todas las sesiones activas anteriores en otros dispositivos han sido cerradas automáticamente.</p>
        <hr style='border: 0; border-top: 1px solid #2E2856; margin: 20px 0;'/>
        <p style='color: #64748B; font-size: 11px; text-align: center;'>© 2026 SysPuntoVenta. Todos los derechos reservados.</p>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, html);
        }
    }
}

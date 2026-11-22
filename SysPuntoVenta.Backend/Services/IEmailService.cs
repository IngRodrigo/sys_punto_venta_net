using System.Threading.Tasks;

namespace SysPuntoVenta.Backend.Services
{
    /// <summary>
    /// Interfaz para el envío de correos electrónicos transaccionales y notificaciones de seguridad.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Envía un correo electrónico con cuerpo HTML al destinatario especificado.
        /// </summary>
        /// <param name="toEmail">Dirección de correo de destino.</param>
        /// <param name="subject">Asunto del correo.</param>
        /// <param name="htmlBody">Contenido formateado en HTML.</param>
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);

        /// <summary>
        /// Envía un correo con el código OTP de 6 dígitos para el restablecimiento de contraseña.
        /// </summary>
        /// <param name="toEmail">Correo electrónico del usuario.</param>
        /// <param name="nombre">Nombre del usuario operador.</param>
        /// <param name="codigoOtp">Código numérico temporal de 6 dígitos.</param>
        Task<bool> SendPasswordResetCodeAsync(string toEmail, string nombre, string codigoOtp);

        /// <summary>
        /// Envía una notificación confirmando que la contraseña de la cuenta ha sido modificada.
        /// </summary>
        /// <param name="toEmail">Correo electrónico del usuario.</param>
        /// <param name="nombre">Nombre del usuario.</param>
        /// <param name="ipAddress">Dirección IP desde donde se realizó el cambio.</param>
        Task<bool> SendPasswordChangedNotificationAsync(string toEmail, string nombre, string ipAddress);
    }
}

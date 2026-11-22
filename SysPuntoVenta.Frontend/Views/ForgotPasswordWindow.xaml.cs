using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using SysPuntoVenta.Frontend.Services;

namespace SysPuntoVenta.Frontend.Views
{
    /// <summary>
    /// Ventana modal para la recuperación y reseteo seguro de contraseñas mediante código OTP de 6 dígitos.
    /// </summary>
    public partial class ForgotPasswordWindow : Window
    {
        public ForgotPasswordWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Manejador del botón para enviar el código OTP al correo del usuario.
        /// </summary>
        private async void BtnEnviarCodigo_Click(object sender, RoutedEventArgs e)
        {
            string emailOUser = txtEmailOUser.Text.Trim();
            if (string.IsNullOrWhiteSpace(emailOUser))
            {
                lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                lblMensaje.Text = "Por favor, ingresa tu correo o nombre de usuario.";
                txtEmailOUser.Focus();
                return;
            }

            btnEnviarCodigo.IsEnabled = false;
            lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            lblMensaje.Text = "Solicitando código de seguridad...";

            try
            {
                var resp = await ApiClient.ForgotPasswordAsync(emailOUser);
                lblMensaje.Text = "";

                // Transición al Paso 2
                panelPaso1.Visibility = Visibility.Collapsed;
                panelPaso2.Visibility = Visibility.Visible;
                lblInfoEnvio.Text = resp.Mensaje;
                txtCodigoOtp.Focus();
            }
            catch (Exception ex)
            {
                lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                lblMensaje.Text = $"Error: {ex.Message}";
                btnEnviarCodigo.IsEnabled = true;
            }
        }

        /// <summary>
        /// Manejador del botón para validar el código OTP y aplicar la nueva contraseña con BCrypt en el backend.
        /// </summary>
        private async void BtnRestablecer_Click(object sender, RoutedEventArgs e)
        {
            string emailOUser = txtEmailOUser.Text.Trim();
            string otp = txtCodigoOtp.Text.Trim();
            string pass1 = txtNuevaPass.Password;
            string pass2 = txtConfirmarPass.Password;

            if (string.IsNullOrWhiteSpace(otp) || otp.Length < 6)
            {
                lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                lblMensaje.Text = "Por favor ingresa el código de 6 dígitos recibido por correo.";
                txtCodigoOtp.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(pass1) || pass1.Length < 4)
            {
                lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                lblMensaje.Text = "La contraseña debe tener al menos 4 caracteres.";
                txtNuevaPass.Focus();
                return;
            }

            if (pass1 != pass2)
            {
                lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                lblMensaje.Text = "Las contraseñas no coinciden.";
                txtConfirmarPass.Focus();
                return;
            }

            btnRestablecer.IsEnabled = false;
            lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            lblMensaje.Text = "Restableciendo contraseña...";

            try
            {
                var resp = await ApiClient.ResetPasswordAsync(emailOUser, otp, pass1);

                if (resp.Exito)
                {
                    MessageBox.Show(resp.Mensaje, "Contraseña Restablecida", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    lblMensaje.Text = resp.Mensaje ?? "No se pudo restablecer la contraseña.";
                }
            }
            catch (Exception ex)
            {
                lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                lblMensaje.Text = $"Error: {ex.Message}";
            }
            finally
            {
                btnRestablecer.IsEnabled = true;
            }
        }

        /// <summary>
        /// Cierra la ventana modal de recuperación.
        /// </summary>
        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

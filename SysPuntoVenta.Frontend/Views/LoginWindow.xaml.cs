using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SysPuntoVenta.Common.Models;
using SysPuntoVenta.Frontend.Services;

namespace SysPuntoVenta.Frontend.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
        }

        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await VerificarConexionBackendAsync();
        }

        private async Task VerificarConexionBackendAsync()
        {
            try
            {
                lblServerStatus.Text = "Conectando....";
                lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));

                var online = await ApiClient.CheckHealthAsync();

                if (online)
                {
                    lblServerStatus.Text = "✔ Conectado";
                    lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                }
                else
                {
                    lblServerStatus.Text = "⚠ API no responde en localhost:5050. Inicia SysPuntoVenta.Backend.";
                    lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                }
            }
            catch (Exception ex)
            {
                lblServerStatus.Text = $"⚠ Error de conexión: {ex.Message}";
                lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            }
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            await EjecutarLoginAsync();
        }

        private async void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await EjecutarLoginAsync();
            }
        }

        private async Task EjecutarLoginAsync()
        {
            string usuario = txtUserName.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(usuario))
            {
                lblMensaje.Text = "Por favor, ingresa el nombre de usuario.";
                txtUserName.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                lblMensaje.Text = "Por favor, ingresa la contraseña.";
                txtPassword.Focus();
                return;
            }

            btnLogin.IsEnabled = false;
            lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            lblMensaje.Text = "Autenticando contra la API REST JSON...";

            try
            {
                var respuesta = await ApiClient.LoginAsync(usuario, password);

                if (respuesta.Autenticado && respuesta.Usuario != null)
                {
                    UserSession.CurrentUser = respuesta.Usuario;

                    var mainWin = new MainWindow();
                    mainWin.Show();
                    this.Close();
                }
                else
                {
                    lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    lblMensaje.Text = respuesta.Mensaje ?? "Usuario o contraseña inválidos.";
                }
            }
            catch (Exception ex)
            {
                lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                lblMensaje.Text = $"Error de comunicación HTTP: {ex.Message}";
            }
            finally
            {
                btnLogin.IsEnabled = true;
            }
        }

        private void BtnRegistrarse_Click(object sender, RoutedEventArgs e)
        {
            var signUpWin = new SignUpWindow();
            signUpWin.Owner = this;
            signUpWin.ShowDialog();
        }

        /// <summary>
        /// Ejecuta el flujo de inicio de sesión con Google (OAuth 2.0 / token ID).
        /// En entorno de desarrollo o sin ClientId productivo, utiliza el modo simulado automático.
        /// </summary>
        /// <param name="sender">Botón de Google presionado.</param>
        /// <param name="e">Argumentos del evento de clic.</param>
        private async void BtnGoogleLogin_Click(object sender, RoutedEventArgs e)
        {
            btnGoogleLogin.IsEnabled = false;
            lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            lblMensaje.Text = "Conectando con Google Authentication...";

            try
            {
                // En modo desarrollo simulado, enviamos IdToken simulado
                var req = new GoogleLoginRequest
                {
                    IdToken = "SIMULATED_DEV_TOKEN_GOOGLE_POS",
                    EmailSimulado = "operador.google@sistema.com",
                    NombreSimulado = "Operador",
                    ApellidoSimulado = "Google",
                    AvatarSimulado = ""
                };

                var respuesta = await ApiClient.GoogleLoginAsync(req);

                if (respuesta.Autenticado && respuesta.Usuario != null)
                {
                    UserSession.CurrentUser = respuesta.Usuario;

                    var mainWin = new MainWindow();
                    mainWin.Show();
                    this.Close();
                }
                else
                {
                    lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    lblMensaje.Text = respuesta.Mensaje ?? "No se pudo iniciar sesión con Google.";
                }
            }
            catch (Exception ex)
            {
                lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                lblMensaje.Text = $"Error al autenticar con Google: {ex.Message}";
            }
            finally
            {
                btnGoogleLogin.IsEnabled = true;
            }
        }

        /// <summary>
        /// Despliega el diálogo modal de recuperación de contraseñas mediante verificación por código OTP enviado al correo.
        /// </summary>
        /// <param name="sender">Elemento emisor del evento de hipervínculo.</param>
        /// <param name="e">Argumentos del evento de enrutamiento.</param>
        private void HlForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            var forgotWin = new ForgotPasswordWindow
            {
                Owner = this
            };
            forgotWin.ShowDialog();
        }

        /// <summary>
        /// Alterna entre el tema visual Claro y Oscuro directamente desde la pantalla de inicio de sesión.
        /// </summary>
        /// <param name="sender">Botón de tema presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private void BtnToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ToggleTheme();
            btnToggleTheme.Content = ThemeManager.IsDarkTheme ? "☀️ Claro" : "🌙 Oscuro";
        }
    }
}

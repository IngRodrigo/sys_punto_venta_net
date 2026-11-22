using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using SysPuntoVenta.Common.Models;
using SysPuntoVenta.Frontend.Services;

namespace SysPuntoVenta.Frontend.Views
{
    public partial class SignUpWindow : Window
    {
        public SignUpWindow()
        {
            InitializeComponent();
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text) || 
                string.IsNullOrWhiteSpace(txtApellido.Text) || 
                string.IsNullOrWhiteSpace(txtUsuario.Text) || 
                string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                lblMensaje.Text = "Por favor completa todos los campos obligatorios.";
                return;
            }

            btnGuardar.IsEnabled = false;
            lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            lblMensaje.Text = "Registrando usuario...";

            try
            {
                var resp = await ApiClient.RegistrarUsuarioAsync(new RegistroUsuarioRequest
                {
                    Nombre = txtNombre.Text.Trim(),
                    Apellido = txtApellido.Text.Trim(),
                    UserName = txtUsuario.Text.Trim(),
                    Password = txtPassword.Password,
                    Email = txtEmail.Text.Trim()
                });

                if (resp.Exito)
                {
                    MessageBox.Show(resp.Mensaje, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    lblMensaje.Text = resp.Mensaje ?? "No se pudo registrar el usuario.";
                }
            }
            catch (Exception ex)
            {
                lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                lblMensaje.Text = $"Error: {ex.Message}";
            }
            finally
            {
                btnGuardar.IsEnabled = true;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

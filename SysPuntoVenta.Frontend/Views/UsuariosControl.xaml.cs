using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SysPuntoVenta.Common.Models;
using SysPuntoVenta.Frontend.Services;

namespace SysPuntoVenta.Frontend.Views
{
    public partial class UsuariosControl : UserControl
    {
        public UsuariosControl()
        {
            InitializeComponent();
            Loaded += UsuariosControl_Loaded;
        }

        private async void UsuariosControl_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarUsuariosAsync();
        }

        private async Task CargarUsuariosAsync()
        {
            try
            {
                var lista = await ApiClient.GetUsuariosAsync();
                dgUsuarios.ItemsSource = lista;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar usuarios desde la API: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnNuevoUsuario_Click(object sender, RoutedEventArgs e)
        {
            var win = new SignUpWindow();
            if (Window.GetWindow(this) is Window parentWin)
            {
                win.Owner = parentWin;
            }
            win.ShowDialog();
            await CargarUsuariosAsync();
        }
    }
}

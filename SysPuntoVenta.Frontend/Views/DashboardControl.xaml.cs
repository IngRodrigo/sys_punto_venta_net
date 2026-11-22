using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SysPuntoVenta.Common.Models;
using SysPuntoVenta.Frontend.Services;

namespace SysPuntoVenta.Frontend.Views
{
    public partial class DashboardControl : UserControl
    {
        public DashboardControl()
        {
            InitializeComponent();
            Loaded += DashboardControl_Loaded;
        }

        private async void DashboardControl_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarEstadisticasAsync();
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            await CargarEstadisticasAsync();
        }

        public async Task CargarEstadisticasAsync()
        {
            try
            {
                var stats = await ApiClient.GetDashboardStatsAsync();

                lblCantidadVentas.Text = stats.CantidadVentas.ToString("N0");
                lblTotalRecaudado.Text = $"{stats.TotalRecaudado:N0} Gs.";
                lblCantidadUsuarios.Text = stats.CantidadUsuarios.ToString("N0");
                lblCantidadClientes.Text = stats.CantidadClientes.ToString("N0");

                dgVentasRecientes.ItemsSource = stats.VentasRecientes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar estadísticas del Dashboard: {ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

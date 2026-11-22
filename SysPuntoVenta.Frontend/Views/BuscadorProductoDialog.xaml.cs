using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SysPuntoVenta.Common.Models;
using SysPuntoVenta.Frontend.Services;

namespace SysPuntoVenta.Frontend.Views
{
    public partial class BuscadorProductoDialog : Window
    {
        public ProductoDto? ProductoSeleccionado { get; private set; }

        public BuscadorProductoDialog()
        {
            InitializeComponent();
            Loaded += BuscadorProductoDialog_Loaded;
        }

        private async void BuscadorProductoDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarProductosAsync();
            txtFiltro.Focus();
        }

        private async Task CargarProductosAsync(string filtro = "")
        {
            try
            {
                List<ProductoDto> lista;
                if (string.IsNullOrWhiteSpace(filtro))
                    lista = await ApiClient.GetProductosAsync();
                else
                    lista = await ApiClient.BuscarProductosAsync(filtro);

                dgProductos.ItemsSource = lista;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar productos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            await CargarProductosAsync(txtFiltro.Text.Trim());
        }

        private async void TxtFiltro_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await CargarProductosAsync(txtFiltro.Text.Trim());
            }
        }

        private void BtnSeleccionar_Click(object sender, RoutedEventArgs e)
        {
            SeleccionarYSalir();
        }

        private void DgProductos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SeleccionarYSalir();
        }

        private void SeleccionarYSalir()
        {
            if (dgProductos.SelectedItem is ProductoDto prod)
            {
                ProductoSeleccionado = prod;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Por favor selecciona un producto de la lista.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

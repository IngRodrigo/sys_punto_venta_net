using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SysPuntoVenta.Common.Models;
using SysPuntoVenta.Frontend.Services;

namespace SysPuntoVenta.Frontend.Views
{
    public partial class ProductosControl : UserControl
    {
        private int _idProductoSeleccionado = 0;

        public ProductosControl()
        {
            InitializeComponent();
            Loaded += ProductosControl_Loaded;
        }

        private async void ProductosControl_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarCombosAsync();
            await CargarProductosAsync();
        }

        private async Task CargarCombosAsync()
        {
            try
            {
                var impuestos = await ApiClient.GetImpuestosAsync();
                var proveedores = await ApiClient.GetProveedoresAsync();

                cbImpuesto.ItemsSource = impuestos;
                if (impuestos.Count > 0) cbImpuesto.SelectedIndex = 0;

                cbProveedor.ItemsSource = proveedores;
                if (proveedores.Count > 0) cbProveedor.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar catálogos: {ex.Message}");
            }
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
                MessageBox.Show($"Error al cargar productos desde la API: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtPrecioBruto_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularPrecios();
        }

        private void CalcularPrecios()
        {
            if (decimal.TryParse(txtPrecioBruto.Text, out decimal bruto) && bruto >= 0)
            {
                decimal impuesto = Math.Round(bruto * 0.10m, 2);
                decimal neto = bruto + impuesto;

                txtPrecioImpuesto.Text = impuesto.ToString("N2");
                txtPrecioNeto.Text = neto.ToString("N2");
            }
            else
            {
                txtPrecioImpuesto.Text = "0.00";
                txtPrecioNeto.Text = "0.00";
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text) || string.IsNullOrWhiteSpace(txtDescripcion.Text))
            {
                MessageBox.Show("Debes ingresar el código y la descripción del producto.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrecioBruto.Text, out decimal bruto) || bruto < 0)
            {
                MessageBox.Show("Ingresa un precio bruto válido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrecioBruto.Focus();
                return;
            }

            decimal impuesto = Math.Round(bruto * 0.10m, 2);
            decimal neto = bruto + impuesto;
            int.TryParse(txtCantidad.Text, out int cantidad);

            var prod = new ProductoDto
            {
                Id = _idProductoSeleccionado,
                Codigo = txtCodigo.Text.Trim(),
                Descripcion = txtDescripcion.Text.Trim(),
                Bruto = bruto,
                PrecioImpuesto = impuesto,
                PrecioNeto = neto,
                IdImpuesto = (cbImpuesto.SelectedValue as int?) ?? 1,
                IdProveedor = (cbProveedor.SelectedValue as int?) ?? 1,
                Cantidad = cantidad > 0 ? cantidad : 100
            };

            btnGuardar.IsEnabled = false;

            try
            {
                var resp = await ApiClient.GuardarProductoAsync(prod);

                if (resp.Exito)
                {
                    MessageBox.Show(resp.Mensaje, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    LimpiarFormulario();
                    await CargarProductosAsync();
                }
                else
                {
                    MessageBox.Show(resp.Mensaje ?? "No se pudo guardar el producto.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en la API: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnGuardar.IsEnabled = true;
            }
        }

        private void DgProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgProductos.SelectedItem is ProductoDto prod)
            {
                _idProductoSeleccionado = prod.Id;
                txtCodigo.Text = prod.Codigo;
                txtDescripcion.Text = prod.Descripcion;
                txtPrecioBruto.Text = prod.Bruto.ToString("F2");
                txtPrecioImpuesto.Text = prod.PrecioImpuesto.ToString("F2");
                txtPrecioNeto.Text = prod.PrecioNeto.ToString("F2");
                txtCantidad.Text = prod.Cantidad.ToString();

                if (cbImpuesto.ItemsSource is List<ImpuestoDto>)
                {
                    cbImpuesto.SelectedValue = prod.IdImpuesto;
                }
                if (cbProveedor.ItemsSource is List<ProveedorDto>)
                {
                    cbProveedor.SelectedValue = prod.IdProveedor;
                }

                btnGuardar.Content = "💾 Actualizar Producto";
            }
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            _idProductoSeleccionado = 0;
            txtCodigo.Text = "";
            txtDescripcion.Text = "";
            txtPrecioBruto.Text = "";
            txtPrecioImpuesto.Text = "";
            txtPrecioNeto.Text = "";
            txtCantidad.Text = "100";
            btnGuardar.Content = "💾 Guardar Producto";
            dgProductos.SelectedItem = null;
            txtCodigo.Focus();
        }

        private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            await CargarProductosAsync(txtBuscar.Text.Trim());
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            await CargarProductosAsync(txtBuscar.Text.Trim());
        }

        private async void TxtBuscar_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await CargarProductosAsync(txtBuscar.Text.Trim());
            }
        }
    }
}

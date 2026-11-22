using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SysPuntoVenta.Common.Models;
using SysPuntoVenta.Frontend.Services;

namespace SysPuntoVenta.Frontend.Views
{
    public partial class FacturacionControl : UserControl
    {
        private readonly ObservableCollection<FacturaDetalleDto> _detalles = new();
        private ProductoDto? _productoActual;

        public FacturacionControl()
        {
            InitializeComponent();
            dgDetalle.ItemsSource = _detalles;
            ActualizarTotales();
        }

        private async void TxtDocumentoCliente_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await BuscarClientePorDocumentoAsync(txtDocumentoCliente.Text.Trim());
            }
        }

        private async void BtnBuscarCliente_Click(object sender, RoutedEventArgs e)
        {
            await BuscarClientePorDocumentoAsync(txtDocumentoCliente.Text.Trim());
        }

        private async Task BuscarClientePorDocumentoAsync(string doc)
        {
            if (string.IsNullOrWhiteSpace(doc)) return;

            try
            {
                var clientes = await ApiClient.BuscarClientesAsync(doc);
                if (clientes.Count > 0)
                {
                    var c = clientes[0];
                    txtDocumentoCliente.Text = c.Documento;
                    txtNombreCliente.Text = c.Nombre;
                }
                else
                {
                    txtNombreCliente.Text = "Consumidor Final";
                }
            }
            catch
            {
                txtNombreCliente.Text = "Consumidor Final";
            }
        }

        private async void TxtCodigoProducto_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await BuscarPorCodigoAsync(txtCodigoProducto.Text.Trim());
            }
        }

        private async Task BuscarPorCodigoAsync(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return;

            try
            {
                var prods = await ApiClient.BuscarProductosAsync(codigo);
                var prod = prods.FirstOrDefault(p => p.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase)) ?? prods.FirstOrDefault();

                if (prod != null)
                {
                    CargarProductoEnFormulario(prod);
                }
                else
                {
                    lblInfoStock.Text = $"⚠ Producto '{codigo}' no encontrado.";
                    txtDescripcionProducto.Text = "";
                    _productoActual = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar producto: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBuscarProducto_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new BuscadorProductoDialog();
            if (Window.GetWindow(this) is Window parentWin)
            {
                dialog.Owner = parentWin;
            }

            if (dialog.ShowDialog() == true && dialog.ProductoSeleccionado != null)
            {
                CargarProductoEnFormulario(dialog.ProductoSeleccionado);
            }
        }

        private void CargarProductoEnFormulario(ProductoDto prod)
        {
            _productoActual = prod;
            txtCodigoProducto.Text = prod.Codigo;
            txtDescripcionProducto.Text = prod.Descripcion;
            lblInfoStock.Text = $"✔ Stock: {prod.Cantidad} | Precio Neto: {prod.PrecioNeto:N0} Gs. (IVA Incl.)";
            txtCantidad.Text = "1";
            txtCantidad.Focus();
            txtCantidad.SelectAll();
        }

        private void TxtCantidad_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AgregarProductoAlDetalle();
            }
        }

        private void BtnAgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            AgregarProductoAlDetalle();
        }

        private void AgregarProductoAlDetalle()
        {
            if (_productoActual == null)
            {
                MessageBox.Show("Por favor busca o selecciona un producto primero.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtCantidad.Text, out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Ingresa una cantidad válida mayor a cero.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCantidad.Focus();
                return;
            }

            var itemExistente = _detalles.FirstOrDefault(d => d.IdProducto == _productoActual.Id);
            if (itemExistente != null)
            {
                itemExistente.Cantidad += cantidad;
                itemExistente.Subtotal = itemExistente.Cantidad * itemExistente.PrecioUnitario;
                dgDetalle.Items.Refresh();
            }
            else
            {
                var subtotal = cantidad * _productoActual.PrecioNeto;
                _detalles.Add(new FacturaDetalleDto
                {
                    IdProducto = _productoActual.Id,
                    Codigo = _productoActual.Codigo,
                    Descripcion = _productoActual.Descripcion,
                    Cantidad = cantidad,
                    PrecioUnitario = _productoActual.PrecioNeto,
                    Subtotal = subtotal
                });
            }

            _productoActual = null;
            txtCodigoProducto.Text = "";
            txtDescripcionProducto.Text = "";
            txtCantidad.Text = "1";
            lblInfoStock.Text = "Producto añadido al detalle.";
            txtCodigoProducto.Focus();

            ActualizarTotales();
        }

        private void BtnEliminarDetalle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is FacturaDetalleDto item)
            {
                _detalles.Remove(item);
                ActualizarTotales();
            }
        }

        private void ActualizarTotales()
        {
            decimal totalNeto = _detalles.Sum(d => d.Subtotal);
            decimal totalImpuesto = Math.Round(totalNeto / 11m, 2);
            decimal totalBruto = totalNeto - totalImpuesto;

            lblTotalBruto.Text = $"{totalBruto:N0} Gs.";
            lblTotalImpuesto.Text = $"{totalImpuesto:N0} Gs.";
            lblTotalNeto.Text = $"{totalNeto:N0} Gs.";
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            if (_detalles.Count > 0 && MessageBox.Show("¿Deseas vaciar la venta actual?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            _detalles.Clear();
            _productoActual = null;
            txtCodigoProducto.Text = "";
            txtDescripcionProducto.Text = "";
            txtCantidad.Text = "1";
            txtDocumentoCliente.Text = "4444444-1";
            txtNombreCliente.Text = "Consumidor Final";
            lblInfoStock.Text = "Selecciona o digita un producto para agregarlo.";
            ActualizarTotales();
        }

        private async void BtnEmitirFactura_Click(object sender, RoutedEventArgs e)
        {
            if (_detalles.Count == 0)
            {
                MessageBox.Show("Debes agregar al menos un producto a la factura.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDocumentoCliente.Text))
            {
                MessageBox.Show("Por favor ingresa el documento o RUC del cliente.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDocumentoCliente.Focus();
                return;
            }

            btnEmitirFactura.IsEnabled = false;

            try
            {
                decimal totalNeto = _detalles.Sum(d => d.Subtotal);
                decimal totalImpuesto = Math.Round(totalNeto / 11m, 2);
                decimal totalBruto = totalNeto - totalImpuesto;

                var factura = new FacturaDto
                {
                    DocumentoCliente = txtDocumentoCliente.Text.Trim(),
                    NombreCliente = txtNombreCliente.Text.Trim(),
                    TotalBruto = totalBruto,
                    TotalImpuesto = totalImpuesto,
                    TotalNeto = totalNeto,
                    IdUsuario = UserSession.CurrentUser?.Id ?? 1,
                    Detalles = _detalles.ToList()
                };

                var respuesta = await ApiClient.CrearFacturaAsync(factura);

                if (respuesta.Exito)
                {
                    MessageBox.Show($"¡Venta registrada exitosamente!\n{respuesta.Mensaje}\nTotal: {totalNeto:N0} Gs.", 
                                    "Factura Emitida", MessageBoxButton.OK, MessageBoxImage.Information);
                    LimpiarFormulario();
                }
                else
                {
                    MessageBox.Show(respuesta.Mensaje ?? "No se pudo registrar la factura.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al emitir factura: {ex.Message}", "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnEmitirFactura.IsEnabled = true;
            }
        }
    }
}

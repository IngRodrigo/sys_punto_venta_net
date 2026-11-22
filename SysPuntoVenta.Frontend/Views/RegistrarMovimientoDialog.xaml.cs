using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SysPuntoVenta.Common.Models;
using SysPuntoVenta.Frontend.Services;

namespace SysPuntoVenta.Frontend.Views
{
    /// <summary>
    /// Ventana de diálogo modal para registrar operaciones de inventario: compras a proveedores,
    /// ajustes de stock sobrante o mermas y roturas con justificación obligatoria.
    /// </summary>
    public partial class RegistrarMovimientoDialog : Window
    {
        private List<ProductoDto> _productos = new();

        /// <summary>
        /// Constructor principal del diálogo de registro de movimientos.
        /// </summary>
        /// <param name="idProductoPreseleccionado">Identificador opcional del producto a seleccionar por defecto.</param>
        public RegistrarMovimientoDialog(int? idProductoPreseleccionado = null)
        {
            InitializeComponent();
            Loaded += async (s, e) => await InicializarAsync(idProductoPreseleccionado);
        }

        /// <summary>
        /// Carga el catálogo de productos disponibles y preselecciona el indicado en caso de haberlo.
        /// </summary>
        /// <param name="idProducto">Identificador del producto a preseleccionar.</param>
        private async Task InicializarAsync(int? idProducto)
        {
            try
            {
                _productos = await ApiClient.GetProductosAsync();
                
                var items = _productos.Select(p => new
                {
                    p.Id,
                    p.Codigo,
                    p.Descripcion,
                    p.Cantidad,
                    p.CostoCompra,
                    DescripcionLarga = $"[{p.Codigo}] {p.Descripcion} (Stock: {p.Cantidad})"
                }).ToList();

                cbProductos.ItemsSource = items;

                if (idProducto.HasValue && idProducto.Value > 0)
                {
                    cbProductos.SelectedValue = idProducto.Value;
                }
                else if (items.Any())
                {
                    cbProductos.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                lblMensaje.Text = $"Error al cargar productos: {ex.Message}";
            }
        }

        /// <summary>
        /// Actualiza la etiqueta informativa con el stock actual y costo unitario del producto seleccionado.
        /// </summary>
        private void CbProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbProductos.SelectedValue is int idProd)
            {
                var prod = _productos.FirstOrDefault(p => p.Id == idProd);
                if (prod != null)
                {
                    lblStockActualInfo.Text = $"Existencias actuales: {prod.Cantidad} unidades  |  Costo compra actual: {prod.CostoCompra:N0} Gs.";
                    if (decimal.TryParse(txtCostoUnitario.Text, out var currentCosto) && currentCosto <= 0)
                    {
                        txtCostoUnitario.Text = prod.CostoCompra.ToString("0");
                    }
                }
            }
        }

        /// <summary>
        /// Valida los campos ingresados y envía la petición de ajuste de inventario al backend API.
        /// </summary>
        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (cbProductos.SelectedValue is not int idProd || idProd <= 0)
            {
                lblMensaje.Text = "Por favor selecciona un producto de la lista.";
                return;
            }

            if (!int.TryParse(txtCantidad.Text.Trim(), out var cant) || cant <= 0)
            {
                lblMensaje.Text = "La cantidad debe ser un número entero mayor a cero.";
                txtCantidad.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMotivo.Text))
            {
                lblMensaje.Text = "Debes ingresar una justificación o motivo para la operación.";
                txtMotivo.Focus();
                return;
            }

            decimal.TryParse(txtCostoUnitario.Text.Trim(), out var costo);

            var itemTipo = cbTipoMovimiento.SelectedItem as ComboBoxItem;
            string tipo = itemTipo?.Tag?.ToString() ?? "ENTRADA_COMPRA";

            btnGuardar.IsEnabled = false;
            lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            lblMensaje.Text = "Procesando movimiento...";

            try
            {
                var req = new RegistrarAjusteRequest
                {
                    IdProducto = idProd,
                    TipoMovimiento = tipo,
                    Cantidad = cant,
                    CostoUnitario = costo,
                    DocumentoReferencia = txtDocumentoRef.Text.Trim(),
                    Motivo = txtMotivo.Text.Trim(),
                    IdUsuario = UserSession.CurrentUser?.Id ?? 1
                };

                var resp = await ApiClient.RegistrarAjusteInventarioAsync(req);

                if (resp.Exito)
                {
                    MessageBox.Show(resp.Mensaje, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    lblMensaje.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    lblMensaje.Text = resp.Mensaje ?? "No se pudo registrar la operación.";
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

        /// <summary>
        /// Cierra la ventana modal cancelando la operación.
        /// </summary>
        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SysPuntoVenta.Common.Models;
using SysPuntoVenta.Frontend.Services;

namespace SysPuntoVenta.Frontend.Views
{
    /// <summary>
    /// Vista de usuario para el control de inventario, auditoría de Kardex y valorización de stock.
    /// </summary>
    public partial class InventarioControl : UserControl
    {
        private List<InventarioStockDto> _stockOriginal = new();
        private List<InventarioMovimientoDto> _kardexOriginal = new();
        private string _filtroEstadoActual = "TODOS";

        /// <summary>
        /// Constructor principal del control de inventario.
        /// </summary>
        public InventarioControl()
        {
            InitializeComponent();
            Loaded += InventarioControl_Loaded;
        }

        /// <summary>
        /// Inicializa la carga de datos al mostrar la vista en pantalla.
        /// </summary>
        private async void InventarioControl_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarTodoAsync();
        }

        /// <summary>
        /// Ejecuta en paralelo la actualización de las tarjetas KPI, la lista de existencias y el Kardex.
        /// </summary>
        private async Task CargarTodoAsync()
        {
            await Task.WhenAll(CargarResumenAsync(), CargarStockAsync(), CargarKardexAsync());
        }

        /// <summary>
        /// Consulta y enlaza las métricas consolidadas en las tarjetas KPI superiores.
        /// </summary>
        private async Task CargarResumenAsync()
        {
            try
            {
                var resumen = await ApiClient.GetInventarioResumenAsync();
                lblKpiTotalArticulos.Text = resumen.TotalArticulos.ToString();
                lblKpiCriticos.Text = resumen.ArticulosCriticos.ToString();
                lblKpiAgotados.Text = resumen.ArticulosAgotados.ToString();
                lblKpiValorVenta.Text = $"{resumen.ValorTotalVenta:N0} Gs.";
                lblKpiValorCosto.Text = $"Costo: {resumen.ValorTotalCosto:N0} Gs.";
            }
            catch { }
        }

        /// <summary>
        /// Recupera el listado de existencias de artículos desde la API REST.
        /// </summary>
        private async Task CargarStockAsync()
        {
            try
            {
                _stockOriginal = await ApiClient.GetInventarioStockAsync();
                AplicarFiltroStock();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar existencias de inventario: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Recupera el historial de movimientos de inventario (Kardex) desde la API REST.
        /// </summary>
        private async Task CargarKardexAsync()
        {
            try
            {
                _kardexOriginal = await ApiClient.GetInventarioMovimientosAsync();
                AplicarFiltroKardex();
            }
            catch { }
        }

        /// <summary>
        /// Aplica los filtros de búsqueda por texto y por estado (Todos, Críticos, Agotados) a la grilla de stock.
        /// </summary>
        private void AplicarFiltroStock()
        {
            string txt = txtBuscarStock.Text.Trim().ToLowerInvariant();
            var filtrados = _stockOriginal.AsEnumerable();

            if (!string.IsNullOrEmpty(txt))
            {
                filtrados = filtrados.Where(p =>
                    (p.Codigo?.ToLowerInvariant().Contains(txt) ?? false) ||
                    (p.Descripcion?.ToLowerInvariant().Contains(txt) ?? false) ||
                    (p.ProveedorNombre?.ToLowerInvariant().Contains(txt) ?? false));
            }

            if (_filtroEstadoActual == "CRITICOS")
            {
                filtrados = filtrados.Where(p => p.EstadoStock == "CRITICO");
            }
            else if (_filtroEstadoActual == "AGOTADOS")
            {
                filtrados = filtrados.Where(p => p.EstadoStock == "AGOTADO");
            }

            dgStock.ItemsSource = filtrados.ToList();
        }

        /// <summary>
        /// Aplica los filtros de texto y tipo de operación a la grilla del Kardex de auditoría.
        /// </summary>
        private void AplicarFiltroKardex()
        {
            string txt = txtFiltroKardex.Text.Trim().ToLowerInvariant();
            var itemTipo = cbFiltroTipoMov.SelectedItem as ComboBoxItem;
            string tipo = itemTipo?.Tag?.ToString() ?? "";

            var filtrados = _kardexOriginal.AsEnumerable();

            if (!string.IsNullOrEmpty(txt))
            {
                filtrados = filtrados.Where(m =>
                    (m.ProductoCodigo?.ToLowerInvariant().Contains(txt) ?? false) ||
                    (m.ProductoDescripcion?.ToLowerInvariant().Contains(txt) ?? false) ||
                    (m.DocumentoReferencia?.ToLowerInvariant().Contains(txt) ?? false) ||
                    (m.Motivo?.ToLowerInvariant().Contains(txt) ?? false) ||
                    (m.UsuarioNombre?.ToLowerInvariant().Contains(txt) ?? false));
            }

            if (!string.IsNullOrEmpty(tipo))
            {
                filtrados = filtrados.Where(m => m.TipoMovimiento.Equals(tipo, StringComparison.OrdinalIgnoreCase));
            }

            dgKardex.ItemsSource = filtrados.ToList();
        }

        /// <summary>
        /// Abre el diálogo modal para registrar un nuevo movimiento o ajuste de inventario.
        /// </summary>
        private async void BtnNuevoMovimiento_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RegistrarMovimientoDialog();
            if (Window.GetWindow(this) is Window parentWin)
            {
                dialog.Owner = parentWin;
            }

            if (dialog.ShowDialog() == true)
            {
                await CargarTodoAsync();
            }
        }

        /// <summary>
        /// Permite abrir el registro de ajuste preseleccionando el producto al hacer doble clic en una fila de stock.
        /// </summary>
        private async void DgStock_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgStock.SelectedItem is InventarioStockDto item)
            {
                var dialog = new RegistrarMovimientoDialog(item.IdProducto);
                if (Window.GetWindow(this) is Window parentWin)
                {
                    dialog.Owner = parentWin;
                }

                if (dialog.ShowDialog() == true)
                {
                    await CargarTodoAsync();
                }
            }
        }

        /// <summary>
        /// Fuerza la recarga completa de existencias, Kardex y métricas KPI.
        /// </summary>
        private async void BtnRefrescarTodo_Click(object sender, RoutedEventArgs e)
        {
            await CargarTodoAsync();
        }

        /// <summary>
        /// Conmuta la visibilidad para mostrar la pestaña de existencias físicas y alertas.
        /// </summary>
        private void BtnTabStock_Click(object sender, RoutedEventArgs e)
        {
            panelStock.Visibility = Visibility.Visible;
            panelKardex.Visibility = Visibility.Collapsed;
            btnTabStock.Style = (Style)FindResource("BtnPrimary");
            btnTabKardex.Style = (Style)FindResource("BtnOutline");
        }

        /// <summary>
        /// Conmuta la visibilidad para mostrar la pestaña de auditoría cronológica del Kardex.
        /// </summary>
        private void BtnTabKardex_Click(object sender, RoutedEventArgs e)
        {
            panelStock.Visibility = Visibility.Collapsed;
            panelKardex.Visibility = Visibility.Visible;
            btnTabStock.Style = (Style)FindResource("BtnOutline");
            btnTabKardex.Style = (Style)FindResource("BtnPrimary");
        }

        /// <summary>
        /// Filtra la grilla de existencias para mostrar todos los productos.
        /// </summary>
        private void BtnFiltroTodos_Click(object sender, RoutedEventArgs e)
        {
            _filtroEstadoActual = "TODOS";
            ActualizarEstilosFiltros(btnFiltroTodos);
            AplicarFiltroStock();
        }

        /// <summary>
        /// Filtra la grilla de existencias para mostrar exclusivamente productos con stock crítico.
        /// </summary>
        private void BtnFiltroCriticos_Click(object sender, RoutedEventArgs e)
        {
            _filtroEstadoActual = "CRITICOS";
            ActualizarEstilosFiltros(btnFiltroCriticos);
            AplicarFiltroStock();
        }

        /// <summary>
        /// Filtra la grilla de existencias para mostrar exclusivamente artículos con stock agotado (0 unidades).
        /// </summary>
        private void BtnFiltroAgotados_Click(object sender, RoutedEventArgs e)
        {
            _filtroEstadoActual = "AGOTADOS";
            ActualizarEstilosFiltros(btnFiltroAgotados);
            AplicarFiltroStock();
        }

        /// <summary>
        /// Actualiza visualmente el botón de filtro de stock activo.
        /// </summary>
        private void ActualizarEstilosFiltros(Button activo)
        {
            btnFiltroTodos.Style = (Style)FindResource("BtnOutline");
            btnFiltroCriticos.Style = (Style)FindResource("BtnOutline");
            btnFiltroAgotados.Style = (Style)FindResource("BtnOutline");
            activo.Style = (Style)FindResource("BtnPrimary");
        }

        /// <summary>
        /// Disparador del evento de cambio de texto en la caja de búsqueda de existencias.
        /// </summary>
        private void TxtBuscarStock_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltroStock();
        }

        /// <summary>
        /// Disparador del evento de cambio de texto en la caja de búsqueda del Kardex.
        /// </summary>
        private void TxtFiltroKardex_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltroKardex();
        }

        /// <summary>
        /// Disparador del evento al cambiar el tipo de movimiento en el ComboBox del Kardex.
        /// </summary>
        private void CbFiltroTipoMov_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AplicarFiltroKardex();
        }

        /// <summary>
        /// Genera y exporta el informe de existencias y valorización física a un documento PDF profesional.
        /// </summary>
        private void BtnExportarPdfStock_Click(object sender, RoutedEventArgs e)
        {
            var items = (dgStock.ItemsSource as IEnumerable<InventarioStockDto>) ?? _stockOriginal;
            var columnas = new List<ReporteColumna<InventarioStockDto>>
            {
                new() { Titulo = "ID", AnchoRelativo = 0.5f, SelectorValor = p => p.IdProducto.ToString() },
                new() { Titulo = "Código", AnchoRelativo = 1.2f, SelectorValor = p => p.Codigo },
                new() { Titulo = "Descripción", AnchoRelativo = 2.8f, SelectorValor = p => p.Descripcion },
                new() { Titulo = "Proveedor", AnchoRelativo = 1.6f, SelectorValor = p => p.ProveedorNombre },
                new() { Titulo = "Stock", AnchoRelativo = 0.8f, SelectorValor = p => p.Cantidad.ToString() },
                new() { Titulo = "Mínimo", AnchoRelativo = 0.7f, SelectorValor = p => p.StockMinimo.ToString() },
                new() { Titulo = "Estado", AnchoRelativo = 0.9f, SelectorValor = p => p.EstadoStock },
                new() { Titulo = "Costo Unit.", AnchoRelativo = 1.3f, SelectorValor = p => $"{p.CostoCompra:N0} Gs." },
                new() { Titulo = "Precio Venta", AnchoRelativo = 1.3f, SelectorValor = p => $"{p.PrecioNeto:N0} Gs." },
                new() { Titulo = "Valorización", AnchoRelativo = 1.4f, SelectorValor = p => $"{p.ValorTotalVenta:N0} Gs." }
            };

            PdfExportService.ExportarGrilla(
                "INFORME DE EXISTENCIAS Y CONTROL DE INVENTARIO",
                $"Filtro aplicado: {_filtroEstadoActual} | Total artículos listados: {items.Count()}",
                items,
                columnas,
                $"Inventario_Existencias_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                $"Total de artículos en inventario: {items.Count()}");
        }

        /// <summary>
        /// Genera y exporta el historial cronológico de movimientos del Kardex a un documento PDF profesional.
        /// </summary>
        private void BtnExportarPdfKardex_Click(object sender, RoutedEventArgs e)
        {
            var items = (dgKardex.ItemsSource as IEnumerable<InventarioMovimientoDto>) ?? _kardexOriginal;
            var columnas = new List<ReporteColumna<InventarioMovimientoDto>>
            {
                new() { Titulo = "Fecha / Hora", AnchoRelativo = 1.4f, SelectorValor = m => m.Fecha.ToString("dd/MM/yyyy HH:mm") },
                new() { Titulo = "Código", AnchoRelativo = 1.0f, SelectorValor = m => m.ProductoCodigo },
                new() { Titulo = "Descripción", AnchoRelativo = 2.4f, SelectorValor = m => m.ProductoDescripcion },
                new() { Titulo = "Operación", AnchoRelativo = 1.4f, SelectorValor = m => m.TipoMovimiento },
                new() { Titulo = "Cant.", AnchoRelativo = 0.7f, SelectorValor = m => m.Cantidad.ToString() },
                new() { Titulo = "Prev.", AnchoRelativo = 0.7f, SelectorValor = m => m.StockAnterior.ToString() },
                new() { Titulo = "Final", AnchoRelativo = 0.7f, SelectorValor = m => m.StockPosterior.ToString() },
                new() { Titulo = "Doc. Ref.", AnchoRelativo = 1.2f, SelectorValor = m => m.DocumentoReferencia },
                new() { Titulo = "Operador", AnchoRelativo = 1.1f, SelectorValor = m => m.UsuarioNombre },
                new() { Titulo = "Motivo", AnchoRelativo = 2.0f, SelectorValor = m => m.Motivo }
            };

            PdfExportService.ExportarGrilla(
                "AUDITORÍA DE MOVIMIENTOS DE INVENTARIO (KARDEX)",
                $"Reporte oficial de trazabilidad de entradas, salidas y ajustes de stock",
                items,
                columnas,
                $"Kardex_Movimientos_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                $"Total de operaciones registradas: {items.Count()}");
        }
    }
}

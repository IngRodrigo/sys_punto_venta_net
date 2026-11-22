using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SysPuntoVenta.Common.Models;
using SysPuntoVenta.Frontend.Services;

namespace SysPuntoVenta.Frontend.Views
{
    /// <summary>
    /// Vista de usuario para el Módulo de Informes y Auditoría.
    /// Presenta datos consolidados en grillas para Clientes, Usuarios, Productos y Ventas por período temporal (día, semana y año),
    /// con capacidad de filtrado rápido y exportación a documentos PDF profesionales.
    /// </summary>
    public partial class InformesControl : UserControl
    {
        private List<ClienteDto> _clientesOriginales = new();
        private List<UsuarioDto> _usuariosOriginales = new();
        private List<ProductoDto> _productosOriginales = new();
        private string _periodoVentasActual = "dia";

        /// <summary>
        /// Constructor principal del control de informes. Inicializa los componentes visuales.
        /// </summary>
        public InformesControl()
        {
            InitializeComponent();
            Loaded += InformesControl_Loaded;
        }

        /// <summary>
        /// Manejador de evento al cargar el control en la interfaz principal. Inicia por defecto con la sección de Clientes.
        /// </summary>
        /// <param name="sender">Objeto que originó la llamada.</param>
        /// <param name="e">Parámetros del evento de enrutamiento.</param>
        private async void InformesControl_Loaded(object sender, RoutedEventArgs e)
        {
            ActivarPestana(panelClientes, btnTabClientes);
            await CargarInformeClientesAsync();
        }

        // ==========================================
        // NAVEGACIÓN ENTRE PESTAÑAS / SECCIONES
        // ==========================================

        /// <summary>
        /// Alterna a la sección de informe maestro de clientes en grilla.
        /// </summary>
        /// <param name="sender">Botón de pestaña presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void BtnTabClientes_Click(object sender, RoutedEventArgs e)
        {
            ActivarPestana(panelClientes, btnTabClientes);
            if (!_clientesOriginales.Any())
            {
                await CargarInformeClientesAsync();
            }
        }

        /// <summary>
        /// Alterna a la sección de informe de ventas (Día, Semana o Año).
        /// </summary>
        /// <param name="sender">Botón de pestaña presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void BtnTabVentas_Click(object sender, RoutedEventArgs e)
        {
            ActivarPestana(panelVentas, btnTabVentas);
            await CargarInformeVentasAsync(_periodoVentasActual);
        }

        /// <summary>
        /// Alterna a la sección de informe de cuentas de usuarios en grilla.
        /// </summary>
        /// <param name="sender">Botón de pestaña presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void BtnTabUsuarios_Click(object sender, RoutedEventArgs e)
        {
            ActivarPestana(panelUsuarios, btnTabUsuarios);
            if (!_usuariosOriginales.Any())
            {
                await CargarInformeUsuariosAsync();
            }
        }

        /// <summary>
        /// Alterna a la sección de informe del catálogo de productos y stock en grilla.
        /// </summary>
        /// <param name="sender">Botón de pestaña presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void BtnTabProductos_Click(object sender, RoutedEventArgs e)
        {
            ActivarPestana(panelProductos, btnTabProductos);
            if (!_productosOriginales.Any())
            {
                await CargarInformeProductosAsync();
            }
        }

        /// <summary>
        /// Conmuta la visibilidad de los paneles de informe y actualiza los estilos visuales del botón activo.
        /// </summary>
        /// <param name="panelActivo">Panel de grilla a mostrar.</param>
        /// <param name="botonActivo">Botón correspondiente a destacar.</param>
        private void ActivarPestana(Grid panelActivo, Button botonActivo)
        {
            panelVentas.Visibility = Visibility.Collapsed;
            panelClientes.Visibility = Visibility.Collapsed;
            panelUsuarios.Visibility = Visibility.Collapsed;
            panelProductos.Visibility = Visibility.Collapsed;

            var estiloOutline = (Style)FindResource("BtnOutline");
            var estiloPrimary = (Style)FindResource("BtnPrimary");

            btnTabClientes.Style = estiloOutline;
            btnTabVentas.Style = estiloOutline;
            btnTabUsuarios.Style = estiloOutline;
            btnTabProductos.Style = estiloOutline;

            panelActivo.Visibility = Visibility.Visible;
            botonActivo.Style = estiloPrimary;
        }

        // ==========================================
        // INFORME DE CLIENTES (POR DEFECTO)
        // ==========================================

        /// <summary>
        /// Obtiene la lista completa de clientes registrados desde la API y la enlaza a la grilla de clientes.
        /// </summary>
        private async Task CargarInformeClientesAsync()
        {
            lblStatusInforme.Text = "Cargando clientes...";
            try
            {
                _clientesOriginales = await ApiClient.GetClientesAsync();
                AplicarFiltroClientes();
                lblStatusInforme.Text = $"Clientes: {_clientesOriginales.Count} registrados";
            }
            catch (Exception ex)
            {
                lblStatusInforme.Text = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Aplica un filtro de texto instantáneo sobre el documento, nombre o email de la cartera de clientes.
        /// </summary>
        private void AplicarFiltroClientes()
        {
            string txt = txtFiltroCliente.Text.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(txt))
            {
                dgClientes.ItemsSource = _clientesOriginales;
            }
            else
            {
                dgClientes.ItemsSource = _clientesOriginales.Where(c =>
                    (c.Documento?.ToLowerInvariant().Contains(txt) ?? false) ||
                    (c.Nombre?.ToLowerInvariant().Contains(txt) ?? false) ||
                    (c.Email?.ToLowerInvariant().Contains(txt) ?? false)
                ).ToList();
            }
        }

        /// <summary>
        /// Evento disparado al modificar el texto de búsqueda de clientes.
        /// </summary>
        /// <param name="sender">Caja de texto de búsqueda.</param>
        /// <param name="e">Argumentos del evento de texto.</param>
        private void TxtFiltroCliente_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltroClientes();
        }

        /// <summary>
        /// Fuerza la recarga de clientes desde el servidor REST.
        /// </summary>
        /// <param name="sender">Botón de actualización.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void BtnRefrescarClientes_Click(object sender, RoutedEventArgs e)
        {
            await CargarInformeClientesAsync();
        }

        /// <summary>
        /// Exporta la lista filtrada o completa de clientes a un documento PDF profesional.
        /// </summary>
        /// <param name="sender">Botón de exportar a PDF presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private void BtnExportarPdfClientes_Click(object sender, RoutedEventArgs e)
        {
            var itemsAExportar = (dgClientes.ItemsSource as IEnumerable<ClienteDto>) ?? _clientesOriginales;
            var columnas = new List<ReporteColumna<ClienteDto>>
            {
                new() { Titulo = "ID", AnchoRelativo = 0.6f, SelectorValor = c => c.Id.ToString() },
                new() { Titulo = "RUC / C.I.", AnchoRelativo = 1.3f, SelectorValor = c => c.Documento },
                new() { Titulo = "Nombre o Razón Social", AnchoRelativo = 2.5f, SelectorValor = c => c.Nombre },
                new() { Titulo = "Teléfono / WhatsApp", AnchoRelativo = 1.4f, SelectorValor = c => c.Telefono },
                new() { Titulo = "Dirección", AnchoRelativo = 2.2f, SelectorValor = c => c.Direccion },
                new() { Titulo = "Correo Electrónico", AnchoRelativo = 2.0f, SelectorValor = c => c.Email }
            };

            string filtro = string.IsNullOrWhiteSpace(txtFiltroCliente.Text)
                ? "Cartera completa de clientes"
                : $"Filtro aplicado: '{txtFiltroCliente.Text.Trim()}'";

            PdfExportService.ExportarGrilla(
                "REPORTE GENERAL DE CLIENTES",
                filtro,
                itemsAExportar,
                columnas,
                $"Reporte_Clientes_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                $"Total de clientes listados: {itemsAExportar.Count()}");
        }

        // ==========================================
        // INFORME DE VENTAS (DÍA, SEMANA, AÑO)
        // ==========================================

        /// <summary>
        /// Consulta la API REST para obtener el resumen financiero y la grilla de facturas según el período seleccionado.
        /// </summary>
        /// <param name="periodo">Cadena indicativa: 'dia', 'semana' o 'anio'.</param>
        private async Task CargarInformeVentasAsync(string periodo)
        {
            _periodoVentasActual = periodo;
            lblStatusInforme.Text = "Cargando ventas...";

            ActualizarBotonesPeriodo(periodo);

            try
            {
                var resumen = await ApiClient.GetVentasPorPeriodoAsync(periodo);
                dgVentas.ItemsSource = resumen.Ventas;

                lblTotalVentasCant.Text = resumen.TotalVentas.ToString();
                lblTotalBruto.Text = $"{resumen.TotalBruto:N0} Gs.";
                lblTotalImpuesto.Text = $"{resumen.TotalImpuesto:N0} Gs.";
                lblTotalNeto.Text = $"{resumen.TotalNeto:N0} Gs.";

                lblStatusInforme.Text = $"Ventas ({resumen.Periodo}): {resumen.TotalVentas} facturas";
            }
            catch (Exception ex)
            {
                lblStatusInforme.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Error al cargar ventas: {ex.Message}", "Informes", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Actualiza visualmente el resaltado de los botones de período de ventas.
        /// </summary>
        /// <param name="periodo">El período activo actual ('dia', 'semana' o 'anio').</param>
        private void ActualizarBotonesPeriodo(string periodo)
        {
            var estiloOutline = (Style)FindResource("BtnOutline");
            var estiloPrimary = (Style)FindResource("BtnPrimary");

            btnPeriodoDia.Style = estiloOutline;
            btnPeriodoSemana.Style = estiloOutline;
            btnPeriodoAnio.Style = estiloOutline;

            switch (periodo)
            {
                case "semana":
                    btnPeriodoSemana.Style = estiloPrimary;
                    break;
                case "anio":
                    btnPeriodoAnio.Style = estiloPrimary;
                    break;
                default:
                    btnPeriodoDia.Style = estiloPrimary;
                    break;
            }
        }

        /// <summary>
        /// Filtra y carga las ventas correspondientes al día de hoy.
        /// </summary>
        /// <param name="sender">Botón de filtro Hoy/Día.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void BtnPeriodoDia_Click(object sender, RoutedEventArgs e)
        {
            await CargarInformeVentasAsync("dia");
        }

        /// <summary>
        /// Filtra y carga las ventas correspondientes a los últimos 7 días.
        /// </summary>
        /// <param name="sender">Botón de filtro Semana.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void BtnPeriodoSemana_Click(object sender, RoutedEventArgs e)
        {
            await CargarInformeVentasAsync("semana");
        }

        /// <summary>
        /// Filtra y carga las ventas correspondientes al año actual en curso.
        /// </summary>
        /// <param name="sender">Botón de filtro Año.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void BtnPeriodoAnio_Click(object sender, RoutedEventArgs e)
        {
            await CargarInformeVentasAsync("anio");
        }

        /// <summary>
        /// Refresca los datos de ventas para el período actualmente activo.
        /// </summary>
        /// <param name="sender">Botón de refresco.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void BtnRefrescarVentas_Click(object sender, RoutedEventArgs e)
        {
            await CargarInformeVentasAsync(_periodoVentasActual);
        }

        /// <summary>
        /// Exporta el consolidado de comprobantes de ventas para el período seleccionado a un documento PDF profesional.
        /// </summary>
        /// <param name="sender">Botón de exportar a PDF presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private void BtnExportarPdfVentas_Click(object sender, RoutedEventArgs e)
        {
            var ventas = (dgVentas.ItemsSource as IEnumerable<FacturaDto>) ?? new List<FacturaDto>();
            var columnas = new List<ReporteColumna<FacturaDto>>
            {
                new() { Titulo = "N° Factura", AnchoRelativo = 1.3f, SelectorValor = f => f.NumeroFactura },
                new() { Titulo = "Fecha / Hora", AnchoRelativo = 1.5f, SelectorValor = f => f.Fecha.ToString("dd/MM/yyyy HH:mm") },
                new() { Titulo = "RUC / CI", AnchoRelativo = 1.2f, SelectorValor = f => f.DocumentoCliente },
                new() { Titulo = "Cliente", AnchoRelativo = 2.4f, SelectorValor = f => f.NombreCliente },
                new() { Titulo = "Subtotal (Gs)", AnchoRelativo = 1.3f, SelectorValor = f => f.TotalBruto.ToString("N0") },
                new() { Titulo = "IVA 10% (Gs)", AnchoRelativo = 1.2f, SelectorValor = f => f.TotalImpuesto.ToString("N0") },
                new() { Titulo = "Total Neto (Gs)", AnchoRelativo = 1.4f, SelectorValor = f => f.TotalNeto.ToString("N0") },
                new() { Titulo = "Estado", AnchoRelativo = 1.0f, SelectorValor = f => f.Estado }
            };

            string periodoDesc = _periodoVentasActual switch
            {
                "semana" => "Ventas de los últimos 7 días (Semana)",
                "anio" => $"Ventas del año actual ({DateTime.Now.Year})",
                _ => $"Ventas del día de hoy ({DateTime.Now:dd/MM/yyyy})"
            };

            string pie = $"Facturas: {ventas.Count()} | Total Neto: {lblTotalNeto.Text}";

            PdfExportService.ExportarGrilla(
                "INFORME CONSOLIDADO DE VENTAS",
                periodoDesc,
                ventas,
                columnas,
                $"Reporte_Ventas_{_periodoVentasActual}_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                pie);
        }

        // ==========================================
        // INFORME DE USUARIOS
        // ==========================================

        /// <summary>
        /// Obtiene la lista de usuarios del sistema desde la API y la enlaza a la grilla de usuarios.
        /// </summary>
        private async Task CargarInformeUsuariosAsync()
        {
            lblStatusInforme.Text = "Cargando usuarios...";
            try
            {
                _usuariosOriginales = await ApiClient.GetUsuariosAsync();
                AplicarFiltroUsuarios();
                lblStatusInforme.Text = $"Usuarios: {_usuariosOriginales.Count} registrados";
            }
            catch (Exception ex)
            {
                lblStatusInforme.Text = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Aplica un filtro de texto instantáneo sobre el login, nombre o correo de los usuarios.
        /// </summary>
        private void AplicarFiltroUsuarios()
        {
            string txt = txtFiltroUsuario.Text.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(txt))
            {
                dgUsuarios.ItemsSource = _usuariosOriginales;
            }
            else
            {
                dgUsuarios.ItemsSource = _usuariosOriginales.Where(u =>
                    (u.UserName?.ToLowerInvariant().Contains(txt) ?? false) ||
                    (u.Nombre?.ToLowerInvariant().Contains(txt) ?? false) ||
                    (u.Apellido?.ToLowerInvariant().Contains(txt) ?? false) ||
                    (u.Email?.ToLowerInvariant().Contains(txt) ?? false)
                ).ToList();
            }
        }

        /// <summary>
        /// Evento disparado al modificar el texto de búsqueda de usuarios.
        /// </summary>
        /// <param name="sender">Caja de texto de búsqueda.</param>
        /// <param name="e">Argumentos del evento de texto.</param>
        private void TxtFiltroUsuario_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltroUsuarios();
        }

        /// <summary>
        /// Fuerza la recarga de usuarios desde el servidor REST.
        /// </summary>
        /// <param name="sender">Botón de actualización.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void BtnRefrescarUsuarios_Click(object sender, RoutedEventArgs e)
        {
            await CargarInformeUsuariosAsync();
        }

        /// <summary>
        /// Exporta la lista de usuarios y operadores del sistema a un documento PDF profesional.
        /// </summary>
        /// <param name="sender">Botón de exportar a PDF presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private void BtnExportarPdfUsuarios_Click(object sender, RoutedEventArgs e)
        {
            var usuarios = (dgUsuarios.ItemsSource as IEnumerable<UsuarioDto>) ?? _usuariosOriginales;
            var columnas = new List<ReporteColumna<UsuarioDto>>
            {
                new() { Titulo = "ID", AnchoRelativo = 0.6f, SelectorValor = u => u.Id.ToString() },
                new() { Titulo = "Usuario", AnchoRelativo = 1.4f, SelectorValor = u => u.UserName },
                new() { Titulo = "Nombre", AnchoRelativo = 1.5f, SelectorValor = u => u.Nombre },
                new() { Titulo = "Apellido", AnchoRelativo = 1.5f, SelectorValor = u => u.Apellido },
                new() { Titulo = "Correo Electrónico", AnchoRelativo = 2.2f, SelectorValor = u => u.Email },
                new() { Titulo = "Rol / Perfil", AnchoRelativo = 1.4f, SelectorValor = u => u.Rol },
                new() { Titulo = "Ciudad", AnchoRelativo = 1.4f, SelectorValor = u => u.Ciudad }
            };

            PdfExportService.ExportarGrilla(
                "LISTADO DE USUARIOS Y OPERADORES",
                "Registro de cuentas del sistema",
                usuarios,
                columnas,
                $"Reporte_Usuarios_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                $"Total de usuarios: {usuarios.Count()}");
        }

        // ==========================================
        // INFORME DE PRODUCTOS
        // ==========================================

        /// <summary>
        /// Obtiene el catálogo completo de productos y existencias desde la API y la enlaza a la grilla de productos.
        /// </summary>
        private async Task CargarInformeProductosAsync()
        {
            lblStatusInforme.Text = "Cargando catálogo de productos...";
            try
            {
                _productosOriginales = await ApiClient.GetProductosAsync();
                AplicarFiltroProductos();
                lblStatusInforme.Text = $"Productos: {_productosOriginales.Count} artículos en catálogo";
            }
            catch (Exception ex)
            {
                lblStatusInforme.Text = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Aplica un filtro de texto instantáneo sobre el código o descripción del catálogo de productos.
        /// </summary>
        private void AplicarFiltroProductos()
        {
            string txt = txtFiltroProducto.Text.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(txt))
            {
                dgProductos.ItemsSource = _productosOriginales;
            }
            else
            {
                dgProductos.ItemsSource = _productosOriginales.Where(p =>
                    (p.Codigo?.ToLowerInvariant().Contains(txt) ?? false) ||
                    (p.Descripcion?.ToLowerInvariant().Contains(txt) ?? false) ||
                    (p.ProveedorNombre?.ToLowerInvariant().Contains(txt) ?? false)
                ).ToList();
            }
        }

        /// <summary>
        /// Evento disparado al modificar el texto de búsqueda de productos.
        /// </summary>
        /// <param name="sender">Caja de texto de búsqueda.</param>
        /// <param name="e">Argumentos del evento de texto.</param>
        private void TxtFiltroProducto_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltroProductos();
        }

        /// <summary>
        /// Fuerza la recarga de productos desde el servidor REST.
        /// </summary>
        /// <param name="sender">Botón de actualización.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void BtnRefrescarProductos_Click(object sender, RoutedEventArgs e)
        {
            await CargarInformeProductosAsync();
        }

        /// <summary>
        /// Exporta el catálogo de productos y existencias de inventario a un documento PDF profesional.
        /// </summary>
        /// <param name="sender">Botón de exportar a PDF presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private void BtnExportarPdfProductos_Click(object sender, RoutedEventArgs e)
        {
            var productos = (dgProductos.ItemsSource as IEnumerable<ProductoDto>) ?? _productosOriginales;
            var columnas = new List<ReporteColumna<ProductoDto>>
            {
                new() { Titulo = "ID", AnchoRelativo = 0.6f, SelectorValor = p => p.Id.ToString() },
                new() { Titulo = "Código", AnchoRelativo = 1.4f, SelectorValor = p => p.Codigo },
                new() { Titulo = "Descripción", AnchoRelativo = 2.8f, SelectorValor = p => p.Descripcion },
                new() { Titulo = "Proveedor", AnchoRelativo = 1.8f, SelectorValor = p => p.ProveedorNombre },
                new() { Titulo = "Stock", AnchoRelativo = 0.8f, SelectorValor = p => p.Cantidad.ToString() },
                new() { Titulo = "Base Bruto", AnchoRelativo = 1.3f, SelectorValor = p => $"{p.Bruto:N0} Gs." },
                new() { Titulo = "Precio Venta", AnchoRelativo = 1.3f, SelectorValor = p => $"{p.PrecioNeto:N0} Gs." }
            };

            PdfExportService.ExportarGrilla(
                "CATÁLOGO DE PRODUCTOS & STOCK",
                "Listado oficial de artículos e inventario",
                productos,
                columnas,
                $"Reporte_Productos_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                $"Total de artículos: {productos.Count()}");
        }
    }
}

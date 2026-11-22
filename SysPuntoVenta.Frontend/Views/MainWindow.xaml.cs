using System;
using System.Windows;
using System.Windows.Threading;
using SysPuntoVenta.Frontend.Services;

namespace SysPuntoVenta.Frontend.Views
{
    /// <summary>
    /// Ventana principal de la aplicación de punto de venta y facturación.
    /// Administra la navegación entre módulos funcionales, sesión del usuario y reloj del sistema.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer = new();

        /// <summary>
        /// Inicializa los componentes de la ventana principal y arranca el temporizador del reloj.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => ActualizarReloj();
            _timer.Start();

            ThemeManager.ThemeChanged += ActualizarBotonTema;
        }

        /// <summary>
        /// Manejador de evento al cargar la ventana. Inicializa la sesión del usuario, reloj y módulo inicial.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Argumentos del evento de carga.</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var user = UserSession.CurrentUser;
            if (user != null)
            {
                lblUsuarioNombre.Text = $"{user.Nombre} {user.Apellido} ({user.UserName})";
                lblUsuarioRol.Text = string.IsNullOrEmpty(user.Rol) ? "Operador" : user.Rol;
                lblAvatar.Text = string.IsNullOrEmpty(user.Nombre) ? "U" : user.Nombre.Substring(0, 1).ToUpper();
            }

            ActualizarReloj();
            ActualizarBotonTema();
            NavegarADashboard();
        }

        /// <summary>
        /// Actualiza la visualización de la fecha y hora en el encabezado superior.
        /// </summary>
        private void ActualizarReloj()
        {
            var now = DateTime.Now;
            lblFechaHora.Text = $"📅 {now:dd/MM/yyyy}  ⏰ {now:HH:mm:ss}";
        }

        /// <summary>
        /// Actualiza el texto y apariencia del botón de conmutación de tema visual según el estado actual.
        /// </summary>
        private void ActualizarBotonTema()
        {
            btnToggleTheme.Content = ThemeManager.IsDarkTheme ? "☀️ Modo Claro" : "🌙 Modo Oscuro";
        }

        /// <summary>
        /// Alterna entre el tema visual Claro y Oscuro de la aplicación y actualiza inmediatamente la vista activa en pantalla.
        /// </summary>
        /// <param name="sender">Botón de tema presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private void BtnToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ToggleTheme();
            ActualizarBotonTema();

            if (mainContentArea.Content != null)
            {
                var tipoActual = mainContentArea.Content.GetType();
                mainContentArea.Content = Activator.CreateInstance(tipoActual);
            }
        }

        /// <summary>
        /// Navega a la vista del Dashboard y estadísticas generales.
        /// </summary>
        /// <param name="sender">Botón de navegación presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private void BtnNavDashboard_Click(object sender, RoutedEventArgs e)
        {
            NavegarADashboard();
        }

        /// <summary>
        /// Carga el control de usuario del Dashboard en el área principal de contenido.
        /// </summary>
        private void NavegarADashboard()
        {
            lblTituloSeccion.Text = "PANEL DE CONTROL & DASHBOARD";
            mainContentArea.Content = new DashboardControl();
        }

        /// <summary>
        /// Navega a la vista de gestión y cartera de clientes.
        /// </summary>
        /// <param name="sender">Botón de navegación presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private void BtnNavClientes_Click(object sender, RoutedEventArgs e)
        {
            lblTituloSeccion.Text = "GESTIÓN Y REGISTRO DE CLIENTES";
            mainContentArea.Content = new ClientesControl();
        }

        /// <summary>
        /// Navega a la pantalla de emisión de facturas y punto de venta (POS).
        /// </summary>
        /// <param name="sender">Botón de navegación presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private void BtnNavFacturacion_Click(object sender, RoutedEventArgs e)
        {
            lblTituloSeccion.Text = "PUNTO DE VENTA Y FACTURACIÓN";
            mainContentArea.Content = new FacturacionControl();
        }

        /// <summary>
        /// Navega a la vista de administración del catálogo de productos y stock.
        /// </summary>
        /// <param name="sender">Botón de navegación presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private void BtnNavProductos_Click(object sender, RoutedEventArgs e)
        {
            lblTituloSeccion.Text = "CATÁLOGO Y REGISTRO DE PRODUCTOS";
            mainContentArea.Content = new ProductosControl();
        }

        /// <summary>
        /// Navega a la vista del control de inventario, existencias y auditoría de Kardex.
        /// </summary>
        /// <param name="sender">Botón de navegación presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private void BtnNavInventario_Click(object sender, RoutedEventArgs e)
        {
            lblTituloSeccion.Text = "CONTROL DE INVENTARIO & AUDITORÍA KARDEX";
            mainContentArea.Content = new InventarioControl();
        }

        /// <summary>
        /// Navega al nuevo módulo de Informes y Reportes consolidado en grillas.
        /// </summary>
        /// <param name="sender">Botón de navegación presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private void BtnNavInformes_Click(object sender, RoutedEventArgs e)
        {
            lblTituloSeccion.Text = "MÓDULO DE INFORMES & REPORTES";
            mainContentArea.Content = new InformesControl();
        }

        /// <summary>
        /// Navega a la vista de configuración de ciudades y regiones geográficas.
        /// </summary>
        /// <param name="sender">Botón de navegación presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private void BtnNavCiudades_Click(object sender, RoutedEventArgs e)
        {
            lblTituloSeccion.Text = "CONFIGURACIÓN DE CIUDADES";
            mainContentArea.Content = new CiudadesControl();
        }

        /// <summary>
        /// Navega a la vista de administración de usuarios y accesos del sistema.
        /// </summary>
        /// <param name="sender">Botón de navegación presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private void BtnNavUsuarios_Click(object sender, RoutedEventArgs e)
        {
            lblTituloSeccion.Text = "USUARIOS Y ACCESOS DEL SISTEMA";
            mainContentArea.Content = new UsuariosControl();
        }

        /// <summary>
        /// Cierra la sesión activa del usuario, revoca tokens y retorna a la ventana de inicio de sesión.
        /// </summary>
        /// <param name="sender">Botón de salida presionado.</param>
        /// <param name="e">Argumentos del evento.</param>
        private async void BtnSalir_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Estás seguro que deseas cerrar sesión?", "Salir del Sistema", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await ApiClient.LogoutAsync();
                var loginWin = new LoginWindow();
                loginWin.Show();
                this.Close();
            }
        }
    }
}

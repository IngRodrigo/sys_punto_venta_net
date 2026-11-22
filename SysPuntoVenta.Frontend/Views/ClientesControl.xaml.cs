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
    public partial class ClientesControl : UserControl
    {
        private int _idClienteSeleccionado = 0;
        private List<PaisDto> _paises = new();
        private List<CiudadDto> _todasLasCiudades = new();

        public ClientesControl()
        {
            InitializeComponent();
            Loaded += ClientesControl_Loaded;
        }

        private async void ClientesControl_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarCatalogosAsync();
            await CargarClientesAsync();
        }

        /// <summary>
        /// Carga los catálogos de Países y Ciudades desde la API REST para el enlace en cascada.
        /// </summary>
        private async Task CargarCatalogosAsync()
        {
            try
            {
                _paises = await ApiClient.GetPaisesAsync();
                _todasLasCiudades = await ApiClient.GetCiudadesAsync();

                cbPais.ItemsSource = _paises;
                if (_paises.Any())
                {
                    cbPais.SelectedValue = 1; // Paraguay por defecto
                    ActualizarCiudadesPorPais(1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar catálogos geográficos: {ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Filtra y actualiza el ComboBox de ciudades según el país seleccionado.
        /// </summary>
        /// <param name="idPais">Identificador único del país seleccionado.</param>
        private void ActualizarCiudadesPorPais(int idPais)
        {
            var filtradas = _todasLasCiudades.Where(c => c.IdPais == idPais).ToList();
            cbCiudad.ItemsSource = filtradas;
            if (filtradas.Any())
            {
                cbCiudad.SelectedIndex = 0;
            }
            else
            {
                cbCiudad.SelectedIndex = -1;
            }
        }

        /// <summary>
        /// Evento disparado cuando el usuario cambia el país en el ComboBox, actualizando en cascada las ciudades correspondientes.
        /// </summary>
        /// <param name="sender">ComboBox de país.</param>
        /// <param name="e">Argumentos de cambio de selección.</param>
        private void CbPais_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbPais.SelectedValue is int idPais)
            {
                ActualizarCiudadesPorPais(idPais);
            }
        }

        private async Task CargarClientesAsync(string filtro = "")
        {
            try
            {
                List<ClienteDto> lista;
                if (string.IsNullOrWhiteSpace(filtro))
                {
                    lista = await ApiClient.GetClientesAsync();
                }
                else
                {
                    lista = await ApiClient.BuscarClientesAsync(filtro);
                }
                dgClientes.ItemsSource = lista;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar clientes desde la API: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDocumento.Text) || string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("Debes ingresar el Documento / RUC y el Nombre / Razón Social.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int? idPais = cbPais.SelectedValue as int?;
            int? idCiudad = cbCiudad.SelectedValue as int?;

            var cliente = new ClienteDto
            {
                Id = _idClienteSeleccionado,
                Documento = txtDocumento.Text.Trim(),
                Nombre = txtNombre.Text.Trim(),
                Telefono = txtTelefono.Text.Trim(),
                Direccion = txtDireccion.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                IdPais = idPais ?? 1,
                IdCiudad = idCiudad
            };

            btnGuardar.IsEnabled = false;

            try
            {
                var resp = await ApiClient.GuardarClienteAsync(cliente);
                if (resp.Exito)
                {
                    MessageBox.Show(resp.Mensaje, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    LimpiarFormulario();
                    await CargarClientesAsync();
                }
                else
                {
                    MessageBox.Show(resp.Mensaje, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al comunicar con la API: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnGuardar.IsEnabled = true;
            }
        }

        private void DgClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgClientes.SelectedItem is ClienteDto c)
            {
                _idClienteSeleccionado = c.Id;
                txtDocumento.Text = c.Documento;
                txtNombre.Text = c.Nombre;
                txtTelefono.Text = c.Telefono;
                txtDireccion.Text = c.Direccion;
                txtEmail.Text = c.Email;

                int paisId = c.IdPais ?? 1;
                cbPais.SelectedValue = paisId;
                ActualizarCiudadesPorPais(paisId);

                if (c.IdCiudad.HasValue)
                {
                    cbCiudad.SelectedValue = c.IdCiudad.Value;
                }

                btnGuardar.Content = "💾 Actualizar Cliente";
                btnEliminar.IsEnabled = true;
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_idClienteSeleccionado <= 0) return;

            if (MessageBox.Show($"¿Deseas eliminar al cliente '{txtNombre.Text}'?", "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    var resp = await ApiClient.EliminarClienteAsync(_idClienteSeleccionado);
                    if (resp.Exito)
                    {
                        MessageBox.Show("Cliente eliminado con éxito.", "Eliminado", MessageBoxButton.OK, MessageBoxImage.Information);
                        LimpiarFormulario();
                        await CargarClientesAsync();
                    }
                    else
                    {
                        MessageBox.Show(resp.Mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar cliente: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            _idClienteSeleccionado = 0;
            txtDocumento.Text = "";
            txtNombre.Text = "";
            txtTelefono.Text = "";
            txtDireccion.Text = "";
            txtEmail.Text = "";
            cbPais.SelectedValue = 1;
            ActualizarCiudadesPorPais(1);
            btnGuardar.Content = "💾 Guardar Cliente";
            btnEliminar.IsEnabled = false;
            dgClientes.SelectedItem = null;
            txtDocumento.Focus();
        }

        private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            await CargarClientesAsync(txtBuscar.Text.Trim());
        }

        private async void TxtBuscar_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await CargarClientesAsync(txtBuscar.Text.Trim());
            }
        }
    }
}

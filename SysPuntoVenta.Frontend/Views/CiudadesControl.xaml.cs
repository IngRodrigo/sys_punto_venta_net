using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SysPuntoVenta.Common.Models;
using SysPuntoVenta.Frontend.Services;

namespace SysPuntoVenta.Frontend.Views
{
    public partial class CiudadesControl : UserControl
    {
        private int _idSeleccionado = 0;

        public CiudadesControl()
        {
            InitializeComponent();
            Loaded += CiudadesControl_Loaded;
        }

        private async void CiudadesControl_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarPaisesAsync();
            await CargarCiudadesAsync();
        }

        private async Task CargarPaisesAsync()
        {
            try
            {
                var paises = await ApiClient.GetPaisesAsync();
                cbPais.ItemsSource = paises;
                if (paises.Count > 0) cbPais.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al listar países: {ex.Message}");
            }
        }

        private async Task CargarCiudadesAsync()
        {
            try
            {
                var ciudades = await ApiClient.GetCiudadesAsync();
                dgCiudades.ItemsSource = ciudades;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ciudades: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDescripcion.Text))
            {
                MessageBox.Show("Ingresa el nombre de la ciudad.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ciudad = new CiudadDto
            {
                Id = _idSeleccionado,
                Descripcion = txtDescripcion.Text.Trim(),
                IdPais = (cbPais.SelectedValue as int?) ?? 1
            };

            try
            {
                var resp = await ApiClient.GuardarCiudadAsync(ciudad);

                if (resp.Exito)
                {
                    MessageBox.Show(resp.Mensaje, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    Limpiar();
                    await CargarCiudadesAsync();
                }
                else
                {
                    MessageBox.Show(resp.Mensaje ?? "No se pudo guardar la ciudad.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en la API: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgCiudades_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgCiudades.SelectedItem is CiudadDto c)
            {
                _idSeleccionado = c.Id;
                txtDescripcion.Text = c.Descripcion;
                cbPais.SelectedValue = c.IdPais;
                btnGuardar.Content = "💾 Actualizar Ciudad";
            }
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            Limpiar();
        }

        private void Limpiar()
        {
            _idSeleccionado = 0;
            txtDescripcion.Text = "";
            btnGuardar.Content = "💾 Guardar Ciudad";
            dgCiudades.SelectedItem = null;
            txtDescripcion.Focus();
        }
    }
}

using System;
using System.Windows;

namespace SysPuntoVenta.Frontend.Services
{
    /// <summary>
    /// Administrador central de temas visuales para la aplicación WPF.
    /// Permite alternar dinámicamente en tiempo de ejecución entre Tema Oscuro (Cyber Violet) y Tema Claro (Modern Slate).
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>
        /// Indica si el tema actualmente activo es el Tema Oscuro.
        /// </summary>
        public static bool IsDarkTheme { get; private set; } = true;

        /// <summary>
        /// Evento notificador disparado cuando el tema visual ha sido modificado.
        /// </summary>
        public static event Action? ThemeChanged;

        /// <summary>
        /// Aplica el tema seleccionado (Oscuro o Claro) reemplazando el diccionario de recursos en MergedDictionaries de la aplicación WPF.
        /// </summary>
        /// <param name="isDark">True para activar Tema Oscuro, False para Tema Claro.</param>
        public static void ApplyTheme(bool isDark)
        {
            IsDarkTheme = isDark;
            var app = Application.Current;
            if (app == null) return;

            try
            {
                string themeUri = isDark ? "/Themes/DarkTheme.xaml" : "/Themes/LightTheme.xaml";
                var newThemeDict = new ResourceDictionary
                {
                    Source = new Uri(themeUri, UriKind.RelativeOrAbsolute)
                };

                var appResources = app.Resources;
                if (appResources.MergedDictionaries.Count > 0)
                {
                    appResources.MergedDictionaries[0] = newThemeDict;
                }
                else
                {
                    appResources.MergedDictionaries.Add(newThemeDict);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al aplicar tema: {ex.Message}");
            }

            ThemeChanged?.Invoke();
        }

        /// <summary>
        /// Conmuta entre Tema Oscuro y Tema Claro de forma alternada.
        /// </summary>
        public static void ToggleTheme()
        {
            ApplyTheme(!IsDarkTheme);
        }
    }
}

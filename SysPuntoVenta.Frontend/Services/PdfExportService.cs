using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SysPuntoVenta.Frontend.Services
{
    /// <summary>
    /// Configuración de metadatos y selector de datos para una columna en la exportación de reportes PDF.
    /// </summary>
    /// <typeparam name="T">Tipo del modelo de datos de la fila.</typeparam>
    public class ReporteColumna<T>
    {
        /// <summary>
        /// Encabezado visible de la columna.
        /// </summary>
        public string Titulo { get; set; } = string.Empty;

        /// <summary>
        /// Factor de ancho relativo de la columna respecto a las demás.
        /// </summary>
        public float AnchoRelativo { get; set; } = 1.0f;

        /// <summary>
        /// Función selectora para extraer la representación en texto del campo para cada elemento.
        /// </summary>
        public Func<T, string> SelectorValor { get; set; } = _ => string.Empty;
    }

    /// <summary>
    /// Servicio centralizado y reutilizable para la generación y exportación de reportes en formato PDF.
    /// Soporta cualquier colección de datos tipada, cabeceras corporativas, paginación y formato de tabla.
    /// </summary>
    public static class PdfExportService
    {
        static PdfExportService()
        {
            // Inicialización de la licencia comunitaria de QuestPDF y compatibilidad de fuentes
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseSystemFonts = true;
            QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
        }

        /// <summary>
        /// Exporta una colección de elementos a un documento PDF profesional con diseño corporativo, encabezado y pie de página.
        /// </summary>
        /// <typeparam name="T">Tipo de los elementos a exportar.</typeparam>
        /// <param name="tituloReporte">Título principal del informe (ej. 'CARTERA DE CLIENTES').</param>
        /// <param name="subtituloFiltro">Descripción del filtro o alcance aplicado.</param>
        /// <param name="items">Colección de datos a listar en el cuerpo del informe.</param>
        /// <param name="columnas">Definición de las columnas, anchos y selectores de datos.</param>
        /// <param name="nombreSugeridoArchivo">Nombre predeterminado propuesto en el diálogo de guardado.</param>
        /// <param name="pieResumen">Texto opcional de resumen para la esquina inferior izquierda.</param>
        /// <returns>True si el archivo fue generado y guardado exitosamente.</returns>
        public static bool ExportarGrilla<T>(
            string tituloReporte,
            string subtituloFiltro,
            IEnumerable<T> items,
            List<ReporteColumna<T>> columnas,
            string nombreSugeridoArchivo,
            string? pieResumen = null)
        {
            var lista = items?.ToList() ?? new List<T>();
            if (!lista.Any())
            {
                MessageBox.Show("No hay registros en la grilla para exportar.", "Exportación PDF", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Guardar Reporte en PDF",
                FileName = nombreSugeridoArchivo,
                DefaultExt = ".pdf",
                Filter = "Documentos PDF (*.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() != true)
            {
                return false;
            }

            string rutaDestino = dialog.FileName;

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(24);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Lato"));

                        // Encabezado corporativo
                        page.Header().BorderBottom(1).BorderColor("#CBD5E1").PaddingBottom(8).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("SYS FACTURACIÓN & GESTIÓN").FontSize(14).Bold().FontColor("#7C3AED");
                                col.Item().Text(tituloReporte).FontSize(12).Bold().FontColor("#0F172A");
                                if (!string.IsNullOrWhiteSpace(subtituloFiltro))
                                {
                                    col.Item().Text(subtituloFiltro).FontSize(8.5f).FontColor("#64748B");
                                }
                            });

                            row.ConstantItem(170).AlignRight().Column(col =>
                            {
                                col.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#64748B");
                                col.Item().Text($"Operador: {UserSession.CurrentUser?.Nombre ?? "Admin"}").FontSize(8).FontColor("#64748B");
                                col.Item().Text("Documento Oficial").FontSize(7.5f).Italic().FontColor("#94A3B8");
                            });
                        });

                        // Tabla de contenido
                        page.Content().PaddingVertical(10).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                foreach (var col in columnas)
                                {
                                    cols.RelativeColumn(col.AnchoRelativo);
                                }
                            });

                            // Cabecera de la tabla
                            table.Header(header =>
                            {
                                foreach (var col in columnas)
                                {
                                    header.Cell()
                                        .Background("#7C3AED")
                                        .PaddingVertical(5)
                                        .PaddingHorizontal(4)
                                        .Text(col.Titulo)
                                        .Bold()
                                        .FontSize(8.5f)
                                        .FontColor(Colors.White);
                                }
                            });

                            // Filas de datos
                            int rowIndex = 0;
                            foreach (var item in lista)
                            {
                                string fondoFila = (rowIndex % 2 == 0) ? "#FFFFFF" : "#F8FAFC";

                                foreach (var col in columnas)
                                {
                                    string valor = col.SelectorValor(item) ?? string.Empty;

                                    table.Cell()
                                        .Background(fondoFila)
                                        .BorderBottom(0.5f)
                                        .BorderColor("#E2E8F0")
                                        .PaddingVertical(4)
                                        .PaddingHorizontal(4)
                                        .Text(valor)
                                        .FontSize(8.5f)
                                        .FontColor("#1E293B");
                                }
                                rowIndex++;
                            }
                        });

                        // Pie de página
                        page.Footer().BorderTop(0.5f).BorderColor("#CBD5E1").PaddingTop(6).Row(row =>
                        {
                            string resumenTexto = pieResumen ?? $"Total de registros listados: {lista.Count}";
                            row.RelativeItem().Text(resumenTexto).FontSize(8).Bold().FontColor("#475569");

                            row.RelativeItem().AlignRight().Text(x =>
                            {
                                x.Span("Página ").FontSize(8).FontColor("#64748B");
                                x.CurrentPageNumber().FontSize(8).Bold().FontColor("#7C3AED");
                                x.Span(" de ").FontSize(8).FontColor("#64748B");
                                x.TotalPages().FontSize(8).FontColor("#64748B");
                            });
                        });
                    });
                }).GeneratePdf(rutaDestino);

                var resultado = MessageBox.Show(
                    $"Reporte generado con éxito en:\n{rutaDestino}\n\n¿Deseas abrir el archivo PDF ahora?",
                    "Exportación Exitosa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (resultado == MessageBoxResult.Yes)
                {
                    AbrirArchivoPdf(rutaDestino);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar el PDF: {ex.Message}", "Error de Exportación", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Inicia el visor de PDF predeterminado del sistema operativo para visualizar el archivo generado.
        /// </summary>
        /// <param name="rutaArchivo">Ruta absoluta del archivo PDF.</param>
        private static void AbrirArchivoPdf(string rutaArchivo)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = rutaArchivo,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir automáticamente el visor de PDF: {ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

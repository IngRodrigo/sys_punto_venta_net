using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using SysPuntoVenta.Backend.Data;
using SysPuntoVenta.Common.Models;

namespace SysPuntoVenta.Backend.Controllers
{
    /// <summary>
    /// Controlador para la generación de reportes e informes del sistema.
    /// Proporciona datos consolidados en grillas para Clientes, Usuarios, Productos y Ventas por período (día, semana y año).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InformesController : ControllerBase
    {
        private readonly Database _database;

        public InformesController(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Obtiene el informe consolidado de ventas filtrado por período temporal: 'dia', 'semana' o 'anio'.
        /// Devuelve los totales calculados y el desglose de cada comprobante emitido.
        /// </summary>
        /// <param name="periodo">Criterio temporal ('dia' para hoy, 'semana' para últimos 7 días, 'anio' para el año actual).</param>
        /// <returns>Objeto VentasPeriodoResumenDto con el resumen financiero y la grilla de ventas.</returns>
        [HttpGet("ventas")]
        public async Task<ActionResult<VentasPeriodoResumenDto>> GetVentasPorPeriodo([FromQuery] string periodo = "dia")
        {
            var pLower = periodo?.ToLowerInvariant() ?? "dia";
            var resumen = new VentasPeriodoResumenDto
            {
                Periodo = pLower switch
                {
                    "semana" => "Última Semana",
                    "anio" => "Año Actual",
                    _ => "Ventas del Día"
                }
            };

            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                string whereClause = pLower switch
                {
                    "semana" => "WHERE f.fecha >= DATE_SUB(NOW(), INTERVAL 7 DAY)",
                    "anio" => "WHERE YEAR(f.fecha) = YEAR(CURDATE())",
                    _ => "WHERE DATE(f.fecha) = CURDATE()"
                };

                string sql = $@"
                    SELECT f.id, f.numero_factura, f.fecha, f.documento_cliente, f.nombre_cliente,
                           f.total_bruto, f.total_impuesto, f.total_neto, f.idUsuario, f.estado
                    FROM facturas f
                    {whereClause}
                    ORDER BY f.fecha DESC;";

                await using var cmd = new MySqlCommand(sql, conn);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var fac = new FacturaDto
                    {
                        Id = r.SafeGetInt32("id"),
                        NumeroFactura = r.SafeGetString("numero_factura"),
                        Fecha = r.SafeGetDateTime("fecha") ?? DateTime.Now,
                        DocumentoCliente = r.SafeGetString("documento_cliente"),
                        NombreCliente = r.SafeGetString("nombre_cliente"),
                        TotalBruto = r.SafeGetDecimal("total_bruto"),
                        TotalImpuesto = r.SafeGetDecimal("total_impuesto"),
                        TotalNeto = r.SafeGetDecimal("total_neto"),
                        IdUsuario = r.SafeGetInt32("idUsuario"),
                        Estado = r.SafeGetString("estado", "EMITIDA")
                    };

                    resumen.Ventas.Add(fac);
                    resumen.TotalBruto += fac.TotalBruto;
                    resumen.TotalImpuesto += fac.TotalImpuesto;
                    resumen.TotalNeto += fac.TotalNeto;
                }

                resumen.TotalVentas = resumen.Ventas.Count;
                return Ok(resumen);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }
    }
}

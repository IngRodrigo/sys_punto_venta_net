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
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly Database _database;

        public DashboardController(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Endpoint público para comprobación de salud y conectividad del servidor (Health Check).
        /// </summary>
        [AllowAnonymous]
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { status = "online", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Obtiene las estadísticas y métricas clave del panel de control (ventas, ingresos, usuarios, clientes y ventas recientes).
        /// Requiere autenticación Bearer Token.
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetStats()
        {
            var stats = new DashboardStatsDto();

            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                // 1. Cantidad de ventas y total recaudado
                const string sqlVentas = "SELECT COUNT(*) AS cant, COALESCE(SUM(total_neto), 0) AS total FROM facturas;";
                await using (var cmd = new MySqlCommand(sqlVentas, conn))
                await using (var r = await cmd.ExecuteReaderAsync())
                {
                    if (await r.ReadAsync())
                    {
                        stats.CantidadVentas = r.SafeGetInt32("cant");
                        stats.TotalRecaudado = r.SafeGetDecimal("total");
                    }
                }

                // 2. Cantidad de usuarios registrados
                const string sqlUsuarios = "SELECT COUNT(*) FROM seg_usuarios;";
                await using (var cmd = new MySqlCommand(sqlUsuarios, conn))
                {
                    stats.CantidadUsuarios = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // 3. Cantidad de clientes registrados
                const string sqlClientes = "SELECT COUNT(*) FROM clientes;";
                await using (var cmd = new MySqlCommand(sqlClientes, conn))
                {
                    stats.CantidadClientes = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // 4. Cantidad de productos en stock/inventario
                const string sqlProductos = "SELECT COUNT(*) FROM productos;";
                await using (var cmd = new MySqlCommand(sqlProductos, conn))
                {
                    stats.CantidadProductos = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // 5. Ventas recientes (Últimas 6 facturas)
                const string sqlRecientes = @"
                    SELECT id, numero_factura, fecha, documento_cliente, nombre_cliente, total_bruto, total_impuesto, total_neto, estado 
                    FROM facturas 
                    ORDER BY id DESC 
                    LIMIT 6;";
                await using (var cmd = new MySqlCommand(sqlRecientes, conn))
                await using (var r = await cmd.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                    {
                        stats.VentasRecientes.Add(new FacturaDto
                        {
                            Id = r.SafeGetInt32("id"),
                            NumeroFactura = r.SafeGetString("numero_factura"),
                            Fecha = r.SafeGetDateTime("fecha") ?? DateTime.Now,
                            DocumentoCliente = r.SafeGetString("documento_cliente"),
                            NombreCliente = r.SafeGetString("nombre_cliente"),
                            TotalBruto = r.SafeGetDecimal("total_bruto"),
                            TotalImpuesto = r.SafeGetDecimal("total_impuesto"),
                            TotalNeto = r.SafeGetDecimal("total_neto"),
                            Estado = r.SafeGetString("estado", "EMITIDA")
                        });
                    }
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al obtener estadísticas: {ex.Message}" });
            }
        }
    }
}

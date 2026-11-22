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
    public class FacturacionController : ControllerBase
    {
        private readonly Database _database;

        public FacturacionController(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Emite una nueva factura de venta dentro de una transacción transaccional atómica,
        /// insertando la cabecera, los ítems del detalle y descontando automáticamente el stock físico de cada producto.
        /// </summary>
        [HttpPost("crear")]
        public async Task<ActionResult<RespuestaOperacion>> CrearFactura([FromBody] FacturaDto factura)
        {
            if (factura.Detalles == null || factura.Detalles.Count == 0)
            {
                return BadRequest(new RespuestaOperacion { Exito = false, Mensaje = "La factura no contiene productos en el detalle" });
            }

            await using var conn = _database.CreateConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            try
            {
                if (string.IsNullOrWhiteSpace(factura.NumeroFactura))
                {
                    const string countSql = "SELECT COUNT(*) + 1 FROM facturas;";
                    await using var countCmd = new MySqlCommand(countSql, conn, trans);
                    var nextNum = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                    factura.NumeroFactura = $"FAC-{nextNum:D6}";
                }

                // 1. Insertar Cabecera
                const string sqlCabecera = @"
                    INSERT INTO facturas (numero_factura, fecha, documento_cliente, nombre_cliente, 
                                          total_bruto, total_impuesto, total_neto, idUsuario, estado)
                    VALUES (@numero_factura, NOW(), @documento_cliente, @nombre_cliente,
                            @total_bruto, @total_impuesto, @total_neto, @idUsuario, 'EMITIDA');
                    SELECT LAST_INSERT_ID();";

                await using var cmdCabecera = new MySqlCommand(sqlCabecera, conn, trans);
                cmdCabecera.Parameters.AddWithValue("@numero_factura", factura.NumeroFactura);
                cmdCabecera.Parameters.AddWithValue("@documento_cliente", factura.DocumentoCliente);
                cmdCabecera.Parameters.AddWithValue("@nombre_cliente", string.IsNullOrWhiteSpace(factura.NombreCliente) ? "Consumidor Final" : factura.NombreCliente);
                cmdCabecera.Parameters.AddWithValue("@total_bruto", factura.TotalBruto);
                cmdCabecera.Parameters.AddWithValue("@total_impuesto", factura.TotalImpuesto);
                cmdCabecera.Parameters.AddWithValue("@total_neto", factura.TotalNeto);
                cmdCabecera.Parameters.AddWithValue("@idUsuario", (object?)factura.IdUsuario ?? 1);

                var idFactura = Convert.ToInt32(await cmdCabecera.ExecuteScalarAsync());

                // 2. Insertar Detalles y Actualizar Stock
                const string sqlDetalle = @"
                    INSERT INTO factura_detalles (idFactura, idProducto, codigo, descripcion, cantidad, precio_unitario, subtotal)
                    VALUES (@idFactura, @idProducto, @codigo, @descripcion, @cantidad, @precio_unitario, @subtotal);";

                const string sqlStock = @"
                    UPDATE productos 
                    SET cantidad = GREATEST(0, cantidad - @cantidad) 
                    WHERE id = @idProducto;";

                foreach (var item in factura.Detalles)
                {
                    await using (var cmdDetalle = new MySqlCommand(sqlDetalle, conn, trans))
                    {
                        cmdDetalle.Parameters.AddWithValue("@idFactura", idFactura);
                        cmdDetalle.Parameters.AddWithValue("@idProducto", item.IdProducto);
                        cmdDetalle.Parameters.AddWithValue("@codigo", item.Codigo);
                        cmdDetalle.Parameters.AddWithValue("@descripcion", item.Descripcion);
                        cmdDetalle.Parameters.AddWithValue("@cantidad", item.Cantidad);
                        cmdDetalle.Parameters.AddWithValue("@precio_unitario", item.PrecioUnitario);
                        cmdDetalle.Parameters.AddWithValue("@subtotal", item.Subtotal);
                        await cmdDetalle.ExecuteNonQueryAsync();
                    }

                    // Obtener stock actual antes del descuento para trazabilidad en Kardex
                    int stockActual = 0;
                    const string sqlStockPrev = "SELECT cantidad FROM productos WHERE id = @idProducto FOR UPDATE;";
                    await using (var cmdStockPrev = new MySqlCommand(sqlStockPrev, conn, trans))
                    {
                        cmdStockPrev.Parameters.AddWithValue("@idProducto", item.IdProducto);
                        var stockObj = await cmdStockPrev.ExecuteScalarAsync();
                        stockActual = stockObj != null ? Convert.ToInt32(stockObj) : 0;
                    }

                    int stockPosterior = Math.Max(0, stockActual - item.Cantidad);

                    await using (var cmdStock = new MySqlCommand(sqlStock, conn, trans))
                    {
                        cmdStock.Parameters.AddWithValue("@cantidad", item.Cantidad);
                        cmdStock.Parameters.AddWithValue("@idProducto", item.IdProducto);
                        await cmdStock.ExecuteNonQueryAsync();
                    }

                    // Registrar movimiento en inventario_movimientos (Kardex)
                    const string sqlKardex = @"
                        INSERT INTO inventario_movimientos 
                            (idProducto, tipo_movimiento, cantidad, stock_anterior, stock_posterior, 
                             costo_unitario, documento_referencia, motivo, idUsuario, fecha)
                        VALUES 
                            (@idProducto, 'SALIDA_VENTA', @cantidad, @stock_anterior, @stock_posterior,
                             @costo, @docRef, @motivo, @idUsuario, NOW());";
                    await using (var cmdKardex = new MySqlCommand(sqlKardex, conn, trans))
                    {
                        cmdKardex.Parameters.AddWithValue("@idProducto", item.IdProducto);
                        cmdKardex.Parameters.AddWithValue("@cantidad", item.Cantidad);
                        cmdKardex.Parameters.AddWithValue("@stock_anterior", stockActual);
                        cmdKardex.Parameters.AddWithValue("@stock_posterior", stockPosterior);
                        cmdKardex.Parameters.AddWithValue("@costo", item.PrecioUnitario);
                        cmdKardex.Parameters.AddWithValue("@docRef", factura.NumeroFactura);
                        cmdKardex.Parameters.AddWithValue("@motivo", $"Venta en POS ({factura.NumeroFactura})");
                        cmdKardex.Parameters.AddWithValue("@idUsuario", (object?)factura.IdUsuario ?? 1);
                        await cmdKardex.ExecuteNonQueryAsync();
                    }
                }

                await trans.CommitAsync();
                return Ok(new RespuestaOperacion
                {
                    Exito = true,
                    Mensaje = $"Factura {factura.NumeroFactura} registrada exitosamente",
                    IdGenerado = idFactura
                });
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                return StatusCode(500, new RespuestaOperacion { Exito = false, Mensaje = $"Error al emitir factura: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtiene el historial de comprobantes de ventas emitidos en orden descendente.
        /// </summary>
        [HttpGet("listar")]
        public async Task<ActionResult<List<FacturaDto>>> ObtenerFacturas()
        {
            var list = new List<FacturaDto>();
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string sql = @"
                    SELECT id, numero_factura, fecha, documento_cliente, nombre_cliente,
                           total_bruto, total_impuesto, total_neto, idUsuario, estado
                    FROM facturas
                    ORDER BY id DESC;";

                await using var cmd = new MySqlCommand(sql, conn);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new FacturaDto
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
                    });
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene una factura específica por su ID numérico incluyendo todos los ítems de su detalle.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<FacturaDto>> ObtenerPorId(int id)
        {
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string sql = @"
                    SELECT id, numero_factura, fecha, documento_cliente, nombre_cliente,
                           total_bruto, total_impuesto, total_neto, idUsuario, estado
                    FROM facturas
                    WHERE id = @id LIMIT 1;";

                FacturaDto? factura = null;
                await using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    await using var r = await cmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        factura = new FacturaDto
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
                    }
                }

                if (factura == null) return NotFound();

                const string sqlDet = "SELECT id, idFactura, idProducto, codigo, descripcion, cantidad, precio_unitario, subtotal FROM factura_detalles WHERE idFactura = @idFactura;";
                await using (var cmdDet = new MySqlCommand(sqlDet, conn))
                {
                    cmdDet.Parameters.AddWithValue("@idFactura", factura.Id);
                    await using var rDet = await cmdDet.ExecuteReaderAsync();
                    while (await rDet.ReadAsync())
                    {
                        factura.Detalles.Add(new FacturaDetalleDto
                        {
                            Id = rDet.SafeGetInt32("id"),
                            IdFactura = rDet.SafeGetInt32("idFactura"),
                            IdProducto = rDet.SafeGetInt32("idProducto"),
                            Codigo = rDet.SafeGetString("codigo"),
                            Descripcion = rDet.SafeGetString("descripcion"),
                            Cantidad = rDet.SafeGetInt32("cantidad"),
                            PrecioUnitario = rDet.SafeGetDecimal("precio_unitario"),
                            Subtotal = rDet.SafeGetDecimal("subtotal")
                        });
                    }
                }

                return Ok(factura);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }
    }
}

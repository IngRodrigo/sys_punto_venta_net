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
    /// Controlador para la gestión y auditoría del inventario de productos,
    /// registro de Kardex de movimientos (entradas, salidas, ajustes) y alertas de existencias.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InventarioController : ControllerBase
    {
        private readonly Database _database;

        public InventarioController(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Obtiene el resumen consolidado de inventario con métricas de artículos críticos, agotados y valorización.
        /// </summary>
        /// <returns>Objeto con las métricas cuantitativas y financieras del inventario.</returns>
        [HttpGet("resumen")]
        public async Task<ActionResult<InventarioResumenDto>> GetResumen()
        {
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string sql = @"
                    SELECT 
                        COUNT(*) AS totalArticulos,
                        COALESCE(SUM(CASE WHEN cantidad > stock_minimo THEN 1 ELSE 0 END), 0) AS articulosNormal,
                        COALESCE(SUM(CASE WHEN cantidad <= stock_minimo AND cantidad > 0 THEN 1 ELSE 0 END), 0) AS articulosCriticos,
                        COALESCE(SUM(CASE WHEN cantidad <= 0 THEN 1 ELSE 0 END), 0) AS articulosAgotados,
                        COALESCE(SUM(cantidad * costo_compra), 0) AS valorTotalCosto,
                        COALESCE(SUM(cantidad * precio_neto), 0) AS valorTotalVenta
                    FROM productos;";

                await using var cmd = new MySqlCommand(sql, conn);
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    return Ok(new InventarioResumenDto
                    {
                        TotalArticulos = r.SafeGetInt32("totalArticulos"),
                        ArticulosNormal = r.SafeGetInt32("articulosNormal"),
                        ArticulosCriticos = r.SafeGetInt32("articulosCriticos"),
                        ArticulosAgotados = r.SafeGetInt32("articulosAgotados"),
                        ValorTotalCosto = r.SafeGetDecimal("valorTotalCosto"),
                        ValorTotalVenta = r.SafeGetDecimal("valorTotalVenta")
                    });
                }

                return Ok(new InventarioResumenDto());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al obtener resumen de inventario: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtiene el listado general de existencias físicas con estado visual de semáforo (Normal, Crítico, Agotado).
        /// </summary>
        /// <returns>Lista de existencias detalladas por artículo.</returns>
        [HttpGet("stock")]
        public async Task<ActionResult<List<InventarioStockDto>>> GetStock()
        {
            var list = new List<InventarioStockDto>();
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string sql = @"
                    SELECT 
                        p.id,
                        COALESCE(p.codigo, '') AS codigo,
                        COALESCE(p.descripcion, '') AS descripcion,
                        COALESCE(p.cantidad, 0) AS cantidad,
                        COALESCE(p.stock_minimo, 5) AS stock_minimo,
                        COALESCE(p.stock_maximo, 100) AS stock_maximo,
                        COALESCE(p.costo_compra, 0) AS costo_compra,
                        COALESCE(p.precio_neto, 0) AS precio_neto,
                        (COALESCE(p.cantidad, 0) * COALESCE(p.costo_compra, 0)) AS valorCosto,
                        (COALESCE(p.cantidad, 0) * COALESCE(p.precio_neto, 0)) AS valorVenta,
                        COALESCE(pr.razon_social, 'Sin Proveedor') AS proveedor,
                        CASE 
                            WHEN COALESCE(p.cantidad, 0) <= 0 THEN 'AGOTADO'
                            WHEN COALESCE(p.cantidad, 0) <= COALESCE(p.stock_minimo, 5) THEN 'CRITICO'
                            ELSE 'NORMAL'
                        END AS estadoStock
                    FROM productos p
                    LEFT JOIN proveedores pr ON p.idProveedor = pr.id
                    ORDER BY 
                        CASE 
                            WHEN COALESCE(p.cantidad, 0) <= 0 THEN 1
                            WHEN COALESCE(p.cantidad, 0) <= COALESCE(p.stock_minimo, 5) THEN 2
                            ELSE 3
                        END ASC,
                        p.descripcion ASC;";

                await using var cmd = new MySqlCommand(sql, conn);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new InventarioStockDto
                    {
                        IdProducto = r.SafeGetInt32("id"),
                        Codigo = r.SafeGetString("codigo"),
                        Descripcion = r.SafeGetString("descripcion"),
                        Cantidad = r.SafeGetInt32("cantidad"),
                        StockMinimo = r.SafeGetInt32("stock_minimo", 5),
                        StockMaximo = r.SafeGetInt32("stock_maximo", 100),
                        CostoCompra = r.SafeGetDecimal("costo_compra"),
                        PrecioNeto = r.SafeGetDecimal("precio_neto"),
                        ValorTotalCosto = r.SafeGetDecimal("valorCosto"),
                        ValorTotalVenta = r.SafeGetDecimal("valorVenta"),
                        ProveedorNombre = r.SafeGetString("proveedor"),
                        EstadoStock = r.SafeGetString("estadoStock", "NORMAL")
                    });
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al obtener existencias: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtiene el historial completo de Kardex (entradas y salidas) para un producto específico.
        /// </summary>
        /// <param name="idProducto">Identificador del producto a auditar.</param>
        /// <returns>Lista cronológica descendente de movimientos registrados para el producto.</returns>
        [HttpGet("kardex/{idProducto}")]
        public async Task<ActionResult<List<InventarioMovimientoDto>>> GetKardex(int idProducto)
        {
            var list = new List<InventarioMovimientoDto>();
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string sql = @"
                    SELECT 
                        m.id,
                        m.idProducto,
                        COALESCE(p.codigo, '') AS codigo,
                        COALESCE(p.descripcion, '') AS descripcion,
                        m.tipo_movimiento,
                        m.cantidad,
                        m.stock_anterior,
                        m.stock_posterior,
                        m.costo_unitario,
                        COALESCE(m.documento_referencia, '') AS documento_referencia,
                        COALESCE(m.motivo, '') AS motivo,
                        COALESCE(u.nombre, 'Sistema') AS usuarioNombre,
                        m.fecha
                    FROM inventario_movimientos m
                    JOIN productos p ON m.idProducto = p.id
                    LEFT JOIN seg_usuarios u ON m.idUsuario = u.id
                    WHERE m.idProducto = @idProducto
                    ORDER BY m.fecha DESC, m.id DESC;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@idProducto", idProducto);

                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new InventarioMovimientoDto
                    {
                        Id = r.SafeGetInt32("id"),
                        IdProducto = r.SafeGetInt32("idProducto"),
                        ProductoCodigo = r.SafeGetString("codigo"),
                        ProductoDescripcion = r.SafeGetString("descripcion"),
                        TipoMovimiento = r.SafeGetString("tipo_movimiento"),
                        Cantidad = r.SafeGetInt32("cantidad"),
                        StockAnterior = r.SafeGetInt32("stock_anterior"),
                        StockPosterior = r.SafeGetInt32("stock_posterior"),
                        CostoUnitario = r.SafeGetDecimal("costo_unitario"),
                        DocumentoReferencia = r.SafeGetString("documento_referencia"),
                        Motivo = r.SafeGetString("motivo"),
                        UsuarioNombre = r.SafeGetString("usuarioNombre"),
                        Fecha = r.SafeGetDateTime("fecha") ?? DateTime.Now
                    });
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al consultar Kardex: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtiene el listado global de movimientos de inventario con filtros opcionales por tipo de operación o producto.
        /// </summary>
        /// <param name="tipo">Filtro por tipo de movimiento (ej: ENTRADA_COMPRA, SALIDA_VENTA, etc.).</param>
        /// <param name="idProducto">Filtro por ID de producto específico.</param>
        /// <returns>Lista de movimientos de inventario encontrados.</returns>
        [HttpGet("movimientos")]
        public async Task<ActionResult<List<InventarioMovimientoDto>>> GetMovimientos([FromQuery] string? tipo, [FromQuery] int? idProducto)
        {
            var list = new List<InventarioMovimientoDto>();
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                string sql = @"
                    SELECT 
                        m.id,
                        m.idProducto,
                        COALESCE(p.codigo, '') AS codigo,
                        COALESCE(p.descripcion, '') AS descripcion,
                        m.tipo_movimiento,
                        m.cantidad,
                        m.stock_anterior,
                        m.stock_posterior,
                        m.costo_unitario,
                        COALESCE(m.documento_referencia, '') AS documento_referencia,
                        COALESCE(m.motivo, '') AS motivo,
                        COALESCE(u.nombre, 'Sistema') AS usuarioNombre,
                        m.fecha
                    FROM inventario_movimientos m
                    JOIN productos p ON m.idProducto = p.id
                    LEFT JOIN seg_usuarios u ON m.idUsuario = u.id
                    WHERE 1 = 1";

                if (!string.IsNullOrWhiteSpace(tipo))
                {
                    sql += " AND m.tipo_movimiento = @tipo";
                }
                if (idProducto.HasValue && idProducto.Value > 0)
                {
                    sql += " AND m.idProducto = @idProducto";
                }

                sql += " ORDER BY m.fecha DESC, m.id DESC LIMIT 200;";

                await using var cmd = new MySqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(tipo)) cmd.Parameters.AddWithValue("@tipo", tipo);
                if (idProducto.HasValue && idProducto.Value > 0) cmd.Parameters.AddWithValue("@idProducto", idProducto.Value);

                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new InventarioMovimientoDto
                    {
                        Id = r.SafeGetInt32("id"),
                        IdProducto = r.SafeGetInt32("idProducto"),
                        ProductoCodigo = r.SafeGetString("codigo"),
                        ProductoDescripcion = r.SafeGetString("descripcion"),
                        TipoMovimiento = r.SafeGetString("tipo_movimiento"),
                        Cantidad = r.SafeGetInt32("cantidad"),
                        StockAnterior = r.SafeGetInt32("stock_anterior"),
                        StockPosterior = r.SafeGetInt32("stock_posterior"),
                        CostoUnitario = r.SafeGetDecimal("costo_unitario"),
                        DocumentoReferencia = r.SafeGetString("documento_referencia"),
                        Motivo = r.SafeGetString("motivo"),
                        UsuarioNombre = r.SafeGetString("usuarioNombre"),
                        Fecha = r.SafeGetDateTime("fecha") ?? DateTime.Now
                    });
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al consultar movimientos: {ex.Message}" });
            }
        }

        /// <summary>
        /// Registra una operación de inventario (ingreso por compra o ajuste manual de stock)
        /// dentro de una transacción atómica que actualiza la existencia del producto y genera el registro en el Kardex.
        /// </summary>
        /// <param name="req">Petición con los datos del ajuste o compra.</param>
        /// <returns>Resultado de la operación con el ID generado en el Kardex.</returns>
        [HttpPost("ajuste")]
        public async Task<ActionResult<RespuestaOperacion>> RegistrarAjuste([FromBody] RegistrarAjusteRequest req)
        {
            if (req.IdProducto <= 0 || req.Cantidad <= 0)
            {
                return BadRequest(new RespuestaOperacion { Exito = false, Mensaje = "El producto y la cantidad deben ser válidos." });
            }

            if (string.IsNullOrWhiteSpace(req.Motivo))
            {
                return BadRequest(new RespuestaOperacion { Exito = false, Mensaje = "El motivo o justificación del movimiento es obligatorio." });
            }

            await using var conn = _database.CreateConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            try
            {
                // 1. Obtener stock actual con bloqueo exclusivo (FOR UPDATE)
                const string sqlSelect = "SELECT cantidad, costo_compra FROM productos WHERE id = @idProducto FOR UPDATE;";
                int stockActual = 0;
                decimal costoActual = 0;

                await using (var cmdSelect = new MySqlCommand(sqlSelect, conn, trans))
                {
                    cmdSelect.Parameters.AddWithValue("@idProducto", req.IdProducto);
                    await using var r = await cmdSelect.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        stockActual = r.SafeGetInt32("cantidad");
                        costoActual = r.SafeGetDecimal("costo_compra");
                    }
                    else
                    {
                        await trans.RollbackAsync();
                        return NotFound(new RespuestaOperacion { Exito = false, Mensaje = "Producto no encontrado." });
                    }
                }

                // 2. Determinar nuevo stock según tipo de movimiento
                int stockPosterior;
                string tipo = req.TipoMovimiento.ToUpperInvariant().Trim();

                if (tipo == "ENTRADA_COMPRA" || tipo == "AJUSTE_POSITIVO" || tipo == "DEVOLUCION_CLIENTE")
                {
                    stockPosterior = stockActual + req.Cantidad;
                }
                else if (tipo == "AJUSTE_NEGATIVO" || tipo == "SALIDA_VENTA")
                {
                    if (stockActual < req.Cantidad)
                    {
                        await trans.RollbackAsync();
                        return BadRequest(new RespuestaOperacion
                        {
                            Exito = false,
                            Mensaje = $"No hay suficiente stock. Stock actual: {stockActual}, solicitado a descontar: {req.Cantidad}."
                        });
                    }
                    stockPosterior = stockActual - req.Cantidad;
                }
                else
                {
                    await trans.RollbackAsync();
                    return BadRequest(new RespuestaOperacion { Exito = false, Mensaje = $"Tipo de movimiento '{req.TipoMovimiento}' no reconocido." });
                }

                // 3. Actualizar productos
                decimal costoFinal = req.CostoUnitario > 0 ? req.CostoUnitario : costoActual;
                const string sqlUpdate = @"
                    UPDATE productos 
                    SET cantidad = @stockPosterior,
                        costo_compra = CASE WHEN @costoFinal > 0 THEN @costoFinal ELSE costo_compra END
                    WHERE id = @idProducto;";

                await using (var cmdUpdate = new MySqlCommand(sqlUpdate, conn, trans))
                {
                    cmdUpdate.Parameters.AddWithValue("@stockPosterior", stockPosterior);
                    cmdUpdate.Parameters.AddWithValue("@costoFinal", costoFinal);
                    cmdUpdate.Parameters.AddWithValue("@idProducto", req.IdProducto);
                    await cmdUpdate.ExecuteNonQueryAsync();
                }

                // 4. Insertar en inventario_movimientos (Kardex)
                const string sqlInsertMov = @"
                    INSERT INTO inventario_movimientos 
                        (idProducto, tipo_movimiento, cantidad, stock_anterior, stock_posterior, 
                         costo_unitario, documento_referencia, motivo, idUsuario, fecha)
                    VALUES 
                        (@idProducto, @tipo, @cantidad, @stockAnterior, @stockPosterior, 
                         @costo, @docRef, @motivo, @idUsuario, NOW());
                    SELECT LAST_INSERT_ID();";

                int nuevoIdMov;
                await using (var cmdInsert = new MySqlCommand(sqlInsertMov, conn, trans))
                {
                    cmdInsert.Parameters.AddWithValue("@idProducto", req.IdProducto);
                    cmdInsert.Parameters.AddWithValue("@tipo", tipo);
                    cmdInsert.Parameters.AddWithValue("@cantidad", req.Cantidad);
                    cmdInsert.Parameters.AddWithValue("@stockAnterior", stockActual);
                    cmdInsert.Parameters.AddWithValue("@stockPosterior", stockPosterior);
                    cmdInsert.Parameters.AddWithValue("@costo", costoFinal);
                    cmdInsert.Parameters.AddWithValue("@docRef", string.IsNullOrWhiteSpace(req.DocumentoReferencia) ? DBNull.Value : req.DocumentoReferencia.Trim());
                    cmdInsert.Parameters.AddWithValue("@motivo", req.Motivo.Trim());
                    cmdInsert.Parameters.AddWithValue("@idUsuario", (object?)req.IdUsuario ?? DBNull.Value);

                    nuevoIdMov = Convert.ToInt32(await cmdInsert.ExecuteScalarAsync());
                }

                await trans.CommitAsync();

                return Ok(new RespuestaOperacion
                {
                    Exito = true,
                    Mensaje = $"Movimiento de inventario registrado con éxito. Nuevo stock: {stockPosterior} unidades.",
                    IdGenerado = nuevoIdMov
                });
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                return StatusCode(500, new RespuestaOperacion
                {
                    Exito = false,
                    Mensaje = $"Error al procesar movimiento de inventario: {ex.Message}"
                });
            }
        }
    }
}

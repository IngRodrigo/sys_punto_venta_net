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
    public class ProductosController : ControllerBase
    {
        private readonly Database _database;

        public ProductosController(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Lista todos los artículos del catálogo de productos con sus impuestos y proveedores.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ProductoDto>>> GetAll()
        {
            var list = new List<ProductoDto>();
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string sql = @"
                    SELECT 
                        p.id, 
                        COALESCE(p.codigo, '') AS codigo, 
                        COALESCE(p.descripcion, '') AS descripcion, 
                        COALESCE(p.bruto, 0) AS bruto, 
                        COALESCE(p.precio_impuesto, 0) AS precio_impuesto, 
                        COALESCE(p.precio_neto, 0) AS precio_neto,
                        COALESCE(p.idImpuesto, 1) AS idImpuesto, 
                        COALESCE(p.cantidad, 0) AS cantidad, 
                        COALESCE(p.stock_minimo, 5) AS stock_minimo,
                        COALESCE(p.stock_maximo, 100) AS stock_maximo,
                        COALESCE(p.costo_compra, 0) AS costo_compra,
                        COALESCE(p.idProveedor, 1) AS idProveedor, 
                        p.create_at,
                        COALESCE(pr.razon_social, 'Sin Proveedor') AS proveedor,
                        COALESCE(i.descripcion, 'IVA 10%') AS impuesto
                    FROM productos p
                    LEFT JOIN proveedores pr ON p.idProveedor = pr.id
                    LEFT JOIN impuestos i ON p.idImpuesto = i.id
                    ORDER BY p.id DESC;";

                await using var cmd = new MySqlCommand(sql, conn);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(MapProducto(r));
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene los detalles de un producto por su identificador único.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductoDto>> GetById(int id)
        {
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string sql = @"
                    SELECT 
                        p.id, COALESCE(p.codigo, '') AS codigo, COALESCE(p.descripcion, '') AS descripcion, 
                        COALESCE(p.bruto, 0) AS bruto, COALESCE(p.precio_impuesto, 0) AS precio_impuesto, 
                        COALESCE(p.precio_neto, 0) AS precio_neto, COALESCE(p.idImpuesto, 1) AS idImpuesto, 
                        COALESCE(p.cantidad, 0) AS cantidad, COALESCE(p.idProveedor, 1) AS idProveedor, 
                        p.create_at, COALESCE(pr.razon_social, 'Sin Proveedor') AS proveedor, COALESCE(i.descripcion, 'IVA 10%') AS impuesto
                    FROM productos p
                    LEFT JOIN proveedores pr ON p.idProveedor = pr.id
                    LEFT JOIN impuestos i ON p.idImpuesto = i.id
                    WHERE p.id = @id LIMIT 1;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    return Ok(MapProducto(r));
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Busca productos en el catálogo filtrando por código o descripción.
        /// </summary>
        [HttpGet("buscar")]
        public async Task<ActionResult<List<ProductoDto>>> Buscar([FromQuery] string q)
        {
            var list = new List<ProductoDto>();
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string sql = @"
                    SELECT 
                        p.id, COALESCE(p.codigo, '') AS codigo, COALESCE(p.descripcion, '') AS descripcion, 
                        COALESCE(p.bruto, 0) AS bruto, COALESCE(p.precio_impuesto, 0) AS precio_impuesto, 
                        COALESCE(p.precio_neto, 0) AS precio_neto, COALESCE(p.idImpuesto, 1) AS idImpuesto, 
                        COALESCE(p.cantidad, 0) AS cantidad, COALESCE(p.idProveedor, 1) AS idProveedor, 
                        p.create_at, COALESCE(pr.razon_social, 'Sin Proveedor') AS proveedor, COALESCE(i.descripcion, 'IVA 10%') AS impuesto
                    FROM productos p
                    LEFT JOIN proveedores pr ON p.idProveedor = pr.id
                    LEFT JOIN impuestos i ON p.idImpuesto = i.id
                    WHERE p.codigo LIKE @filtro OR p.descripcion LIKE @filtro
                    ORDER BY p.id DESC;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@filtro", $"%{q}%");
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(MapProducto(r));
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo producto o actualiza uno existente con cálculo automático de precios e IVA.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<RespuestaOperacion>> Save([FromBody] ProductoDto producto)
        {
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                if (producto.Id <= 0)
                {
                    const string sql = @"
                        INSERT INTO productos (codigo, descripcion, bruto, precio_impuesto, precio_neto, 
                                               idImpuesto, cantidad, stock_minimo, stock_maximo, costo_compra,
                                               idProveedor, create_at, ipCreacion, idUsuario)
                        VALUES (@codigo, @descripcion, @bruto, @precio_impuesto, @precio_neto,
                                @idImpuesto, @cantidad, @stock_minimo, @stock_maximo, @costo_compra,
                                @idProveedor, NOW(), '127.0.0.1', 1);
                        SELECT LAST_INSERT_ID();";

                    await using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@codigo", producto.Codigo);
                    cmd.Parameters.AddWithValue("@descripcion", producto.Descripcion);
                    cmd.Parameters.AddWithValue("@bruto", producto.Bruto);
                    cmd.Parameters.AddWithValue("@precio_impuesto", producto.PrecioImpuesto);
                    cmd.Parameters.AddWithValue("@precio_neto", producto.PrecioNeto);
                    cmd.Parameters.AddWithValue("@idImpuesto", producto.IdImpuesto <= 0 ? 1 : producto.IdImpuesto);
                    cmd.Parameters.AddWithValue("@cantidad", producto.Cantidad <= 0 ? 100 : producto.Cantidad);
                    cmd.Parameters.AddWithValue("@stock_minimo", producto.StockMinimo <= 0 ? 5 : producto.StockMinimo);
                    cmd.Parameters.AddWithValue("@stock_maximo", producto.StockMaximo <= 0 ? 100 : producto.StockMaximo);
                    cmd.Parameters.AddWithValue("@costo_compra", producto.CostoCompra);
                    cmd.Parameters.AddWithValue("@idProveedor", producto.IdProveedor <= 0 ? 1 : producto.IdProveedor);

                    var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return Ok(new RespuestaOperacion { Exito = true, Mensaje = "Producto registrado exitosamente", IdGenerado = id });
                }
                else
                {
                    const string sql = @"
                        UPDATE productos 
                        SET codigo = @codigo, descripcion = @descripcion, bruto = @bruto,
                            precio_impuesto = @precio_impuesto, precio_neto = @precio_neto,
                            idImpuesto = @idImpuesto, cantidad = @cantidad, 
                            stock_minimo = @stock_minimo, stock_maximo = @stock_maximo, costo_compra = @costo_compra,
                            idProveedor = @idProveedor
                        WHERE id = @id;";

                    await using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id", producto.Id);
                    cmd.Parameters.AddWithValue("@codigo", producto.Codigo);
                    cmd.Parameters.AddWithValue("@descripcion", producto.Descripcion);
                    cmd.Parameters.AddWithValue("@bruto", producto.Bruto);
                    cmd.Parameters.AddWithValue("@precio_impuesto", producto.PrecioImpuesto);
                    cmd.Parameters.AddWithValue("@precio_neto", producto.PrecioNeto);
                    cmd.Parameters.AddWithValue("@idImpuesto", producto.IdImpuesto <= 0 ? 1 : producto.IdImpuesto);
                    cmd.Parameters.AddWithValue("@cantidad", producto.Cantidad);
                    cmd.Parameters.AddWithValue("@stock_minimo", producto.StockMinimo <= 0 ? 5 : producto.StockMinimo);
                    cmd.Parameters.AddWithValue("@stock_maximo", producto.StockMaximo <= 0 ? 100 : producto.StockMaximo);
                    cmd.Parameters.AddWithValue("@costo_compra", producto.CostoCompra);
                    cmd.Parameters.AddWithValue("@idProveedor", producto.IdProveedor <= 0 ? 1 : producto.IdProveedor);

                    await cmd.ExecuteNonQueryAsync();
                    return Ok(new RespuestaOperacion { Exito = true, Mensaje = "Producto actualizado exitosamente", IdGenerado = producto.Id });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RespuestaOperacion { Exito = false, Mensaje = $"Error al guardar producto: {ex.Message}" });
            }
        }

        /// <summary>
        /// Mapea de forma segura los campos de una fila del DataReader a un objeto ProductoDto.
        /// </summary>
        private static ProductoDto MapProducto(MySqlDataReader reader)
        {
            return new ProductoDto
            {
                Id = reader.SafeGetInt32("id"),
                Codigo = reader.SafeGetString("codigo"),
                Descripcion = reader.SafeGetString("descripcion"),
                Bruto = reader.SafeGetDecimal("bruto"),
                PrecioImpuesto = reader.SafeGetDecimal("precio_impuesto"),
                PrecioNeto = reader.SafeGetDecimal("precio_neto"),
                IdImpuesto = reader.SafeGetInt32("idImpuesto", 1),
                Cantidad = reader.SafeGetInt32("cantidad", 0),
                StockMinimo = reader.SafeGetInt32("stock_minimo", 5),
                StockMaximo = reader.SafeGetInt32("stock_maximo", 100),
                CostoCompra = reader.SafeGetDecimal("costo_compra", 0),
                IdProveedor = reader.SafeGetInt32("idProveedor", 1),
                ProveedorNombre = reader.SafeGetString("proveedor", "Sin Proveedor"),
                ImpuestoNombre = reader.SafeGetString("impuesto", "IVA 10%"),
                CreateAt = reader.SafeGetDateTime("create_at")
            };
        }
    }
}

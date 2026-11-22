using System;
using System.Collections.Generic;
using CoreWCF;
using MySqlConnector;
using SysPuntoVenta.Backend.Contracts;
using SysPuntoVenta.Backend.Data;
using SysPuntoVenta.Common.Models;

namespace SysPuntoVenta.Backend.Services
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class ProductoService : IProductoService
    {
        private readonly Database _database;

        public ProductoService(Database database)
        {
            _database = database;
        }

        public List<ProductoDto> ObtenerTodos()
        {
            var list = new List<ProductoDto>();
            try
            {
                using var conn = _database.CreateConnection();
                conn.Open();

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
                        COALESCE(p.idProveedor, 1) AS idProveedor, 
                        p.create_at,
                        COALESCE(pr.razon_social, 'Sin Proveedor') AS proveedor,
                        COALESCE(i.descripcion, 'IVA 10%') AS impuesto
                    FROM productos p
                    LEFT JOIN proveedores pr ON p.idProveedor = pr.id
                    LEFT JOIN impuestos i ON p.idImpuesto = i.id
                    ORDER BY p.id DESC;";

                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(MapProducto(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener productos: {ex.Message}");
            }
            return list;
        }

        public ProductoDto? ObtenerPorId(int id)
        {
            try
            {
                using var conn = _database.CreateConnection();
                conn.Open();

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
                        COALESCE(p.idProveedor, 1) AS idProveedor, 
                        p.create_at,
                        COALESCE(pr.razon_social, 'Sin Proveedor') AS proveedor,
                        COALESCE(i.descripcion, 'IVA 10%') AS impuesto
                    FROM productos p
                    LEFT JOIN proveedores pr ON p.idProveedor = pr.id
                    LEFT JOIN impuestos i ON p.idImpuesto = i.id
                    WHERE p.id = @id
                    LIMIT 1;";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapProducto(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener producto por Id: {ex.Message}");
            }
            return null;
        }

        public ProductoDto? ObtenerPorCodigo(string codigo)
        {
            try
            {
                using var conn = _database.CreateConnection();
                conn.Open();

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
                        COALESCE(p.idProveedor, 1) AS idProveedor, 
                        p.create_at,
                        COALESCE(pr.razon_social, 'Sin Proveedor') AS proveedor,
                        COALESCE(i.descripcion, 'IVA 10%') AS impuesto
                    FROM productos p
                    LEFT JOIN proveedores pr ON p.idProveedor = pr.id
                    LEFT JOIN impuestos i ON p.idImpuesto = i.id
                    WHERE p.codigo = @codigo
                    LIMIT 1;";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@codigo", codigo);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapProducto(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener producto por código: {ex.Message}");
            }
            return null;
        }

        public List<ProductoDto> Buscar(string filtro)
        {
            var list = new List<ProductoDto>();
            try
            {
                using var conn = _database.CreateConnection();
                conn.Open();

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
                        COALESCE(p.idProveedor, 1) AS idProveedor, 
                        p.create_at,
                        COALESCE(pr.razon_social, 'Sin Proveedor') AS proveedor,
                        COALESCE(i.descripcion, 'IVA 10%') AS impuesto
                    FROM productos p
                    LEFT JOIN proveedores pr ON p.idProveedor = pr.id
                    LEFT JOIN impuestos i ON p.idImpuesto = i.id
                    WHERE p.codigo LIKE @filtro OR p.descripcion LIKE @filtro
                    ORDER BY p.id DESC;";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@filtro", $"%{filtro}%");
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(MapProducto(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al buscar productos: {ex.Message}");
            }
            return list;
        }

        public RespuestaOperacion GuardarProducto(ProductoDto producto)
        {
            try
            {
                using var conn = _database.CreateConnection();
                conn.Open();

                if (producto.Id <= 0)
                {
                    const string sql = @"
                        INSERT INTO productos (codigo, descripcion, bruto, precio_impuesto, precio_neto, 
                                               idImpuesto, cantidad, idProveedor, create_at, ipCreacion, idUsuario)
                        VALUES (@codigo, @descripcion, @bruto, @precio_impuesto, @precio_neto,
                                @idImpuesto, @cantidad, @idProveedor, NOW(), '127.0.0.1', 1);
                        SELECT LAST_INSERT_ID();";

                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@codigo", producto.Codigo);
                    cmd.Parameters.AddWithValue("@descripcion", producto.Descripcion);
                    cmd.Parameters.AddWithValue("@bruto", producto.Bruto);
                    cmd.Parameters.AddWithValue("@precio_impuesto", producto.PrecioImpuesto);
                    cmd.Parameters.AddWithValue("@precio_neto", producto.PrecioNeto);
                    cmd.Parameters.AddWithValue("@idImpuesto", producto.IdImpuesto <= 0 ? 1 : producto.IdImpuesto);
                    cmd.Parameters.AddWithValue("@cantidad", producto.Cantidad <= 0 ? 100 : producto.Cantidad);
                    cmd.Parameters.AddWithValue("@idProveedor", producto.IdProveedor <= 0 ? 1 : producto.IdProveedor);

                    var id = Convert.ToInt32(cmd.ExecuteScalar());
                    return new RespuestaOperacion { Exito = true, Mensaje = "Producto registrado exitosamente", IdGenerado = id };
                }
                else
                {
                    const string sql = @"
                        UPDATE productos 
                        SET codigo = @codigo, descripcion = @descripcion, bruto = @bruto,
                            precio_impuesto = @precio_impuesto, precio_neto = @precio_neto,
                            idImpuesto = @idImpuesto, cantidad = @cantidad, idProveedor = @idProveedor
                        WHERE id = @id;";

                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id", producto.Id);
                    cmd.Parameters.AddWithValue("@codigo", producto.Codigo);
                    cmd.Parameters.AddWithValue("@descripcion", producto.Descripcion);
                    cmd.Parameters.AddWithValue("@bruto", producto.Bruto);
                    cmd.Parameters.AddWithValue("@precio_impuesto", producto.PrecioImpuesto);
                    cmd.Parameters.AddWithValue("@precio_neto", producto.PrecioNeto);
                    cmd.Parameters.AddWithValue("@idImpuesto", producto.IdImpuesto <= 0 ? 1 : producto.IdImpuesto);
                    cmd.Parameters.AddWithValue("@cantidad", producto.Cantidad);
                    cmd.Parameters.AddWithValue("@idProveedor", producto.IdProveedor <= 0 ? 1 : producto.IdProveedor);

                    cmd.ExecuteNonQuery();
                    return new RespuestaOperacion { Exito = true, Mensaje = "Producto actualizado exitosamente", IdGenerado = producto.Id };
                }
            }
            catch (Exception ex)
            {
                return new RespuestaOperacion { Exito = false, Mensaje = $"Error al guardar producto: {ex.Message}" };
            }
        }

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
                IdProveedor = reader.SafeGetInt32("idProveedor", 1),
                ProveedorNombre = reader.SafeGetString("proveedor", "Sin Proveedor"),
                ImpuestoNombre = reader.SafeGetString("impuesto", "IVA 10%"),
                CreateAt = reader.SafeGetDateTime("create_at")
            };
        }
    }
}

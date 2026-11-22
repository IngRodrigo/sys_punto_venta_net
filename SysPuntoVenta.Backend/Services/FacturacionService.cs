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
    public class FacturacionService : IFacturacionService
    {
        private readonly Database _database;

        public FacturacionService(Database database)
        {
            _database = database;
        }

        public RespuestaOperacion CrearFactura(FacturaDto factura)
        {
            if (factura.Detalles == null || factura.Detalles.Count == 0)
            {
                return new RespuestaOperacion { Exito = false, Mensaje = "La factura no contiene productos en el detalle" };
            }

            using var conn = _database.CreateConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // Generar número correlativo si no viene especificado
                if (string.IsNullOrWhiteSpace(factura.NumeroFactura))
                {
                    const string countSql = "SELECT COUNT(*) + 1 FROM facturas;";
                    using var countCmd = new MySqlCommand(countSql, conn, trans);
                    var nextNum = Convert.ToInt32(countCmd.ExecuteScalar());
                    factura.NumeroFactura = $"FAC-{nextNum:D6}";
                }

                // 1. Insertar Cabecera
                const string sqlCabecera = @"
                    INSERT INTO facturas (numero_factura, fecha, documento_cliente, nombre_cliente, 
                                          total_bruto, total_impuesto, total_neto, idUsuario, estado)
                    VALUES (@numero_factura, NOW(), @documento_cliente, @nombre_cliente,
                            @total_bruto, @total_impuesto, @total_neto, @idUsuario, 'EMITIDA');
                    SELECT LAST_INSERT_ID();";

                using var cmdCabecera = new MySqlCommand(sqlCabecera, conn, trans);
                cmdCabecera.Parameters.AddWithValue("@numero_factura", factura.NumeroFactura);
                cmdCabecera.Parameters.AddWithValue("@documento_cliente", factura.DocumentoCliente);
                cmdCabecera.Parameters.AddWithValue("@nombre_cliente", string.IsNullOrWhiteSpace(factura.NombreCliente) ? "Consumidor Final" : factura.NombreCliente);
                cmdCabecera.Parameters.AddWithValue("@total_bruto", factura.TotalBruto);
                cmdCabecera.Parameters.AddWithValue("@total_impuesto", factura.TotalImpuesto);
                cmdCabecera.Parameters.AddWithValue("@total_neto", factura.TotalNeto);
                cmdCabecera.Parameters.AddWithValue("@idUsuario", (object?)factura.IdUsuario ?? 1);

                var idFactura = Convert.ToInt32(cmdCabecera.ExecuteScalar());

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
                    using (var cmdDetalle = new MySqlCommand(sqlDetalle, conn, trans))
                    {
                        cmdDetalle.Parameters.AddWithValue("@idFactura", idFactura);
                        cmdDetalle.Parameters.AddWithValue("@idProducto", item.IdProducto);
                        cmdDetalle.Parameters.AddWithValue("@codigo", item.Codigo);
                        cmdDetalle.Parameters.AddWithValue("@descripcion", item.Descripcion);
                        cmdDetalle.Parameters.AddWithValue("@cantidad", item.Cantidad);
                        cmdDetalle.Parameters.AddWithValue("@precio_unitario", item.PrecioUnitario);
                        cmdDetalle.Parameters.AddWithValue("@subtotal", item.Subtotal);
                        cmdDetalle.ExecuteNonQuery();
                    }

                    using (var cmdStock = new MySqlCommand(sqlStock, conn, trans))
                    {
                        cmdStock.Parameters.AddWithValue("@cantidad", item.Cantidad);
                        cmdStock.Parameters.AddWithValue("@idProducto", item.IdProducto);
                        cmdStock.ExecuteNonQuery();
                    }
                }

                trans.Commit();
                return new RespuestaOperacion
                {
                    Exito = true,
                    Mensaje = $"Factura {factura.NumeroFactura} registrada exitosamente",
                    IdGenerado = idFactura
                };
            }
            catch (Exception ex)
            {
                trans.Rollback();
                return new RespuestaOperacion { Exito = false, Mensaje = $"Error al emitir factura: {ex.Message}" };
            }
        }

        public List<FacturaDto> ObtenerFacturas()
        {
            var list = new List<FacturaDto>();
            try
            {
                using var conn = _database.CreateConnection();
                conn.Open();

                const string sql = @"
                    SELECT id, numero_factura, fecha, documento_cliente, nombre_cliente,
                           total_bruto, total_impuesto, total_neto, idUsuario, estado
                    FROM facturas
                    ORDER BY id DESC;";

                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new FacturaDto
                    {
                        Id = reader.GetInt32("id"),
                        NumeroFactura = reader.GetString("numero_factura"),
                        Fecha = reader.GetDateTime("fecha"),
                        DocumentoCliente = reader.GetString("documento_cliente"),
                        NombreCliente = reader.IsDBNull(reader.GetOrdinal("nombre_cliente")) ? "" : reader.GetString("nombre_cliente"),
                        TotalBruto = reader.GetDecimal("total_bruto"),
                        TotalImpuesto = reader.GetDecimal("total_impuesto"),
                        TotalNeto = reader.GetDecimal("total_neto"),
                        IdUsuario = reader.IsDBNull(reader.GetOrdinal("idUsuario")) ? null : reader.GetInt32("idUsuario"),
                        Estado = reader.GetString("estado")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al listar facturas: {ex.Message}");
            }
            return list;
        }

        public FacturaDto? ObtenerFacturaPorId(int id)
        {
            try
            {
                using var conn = _database.CreateConnection();
                conn.Open();

                const string sql = @"
                    SELECT id, numero_factura, fecha, documento_cliente, nombre_cliente,
                           total_bruto, total_impuesto, total_neto, idUsuario, estado
                    FROM facturas
                    WHERE id = @id
                    LIMIT 1;";

                FacturaDto? factura = null;
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        factura = new FacturaDto
                        {
                            Id = reader.GetInt32("id"),
                            NumeroFactura = reader.GetString("numero_factura"),
                            Fecha = reader.GetDateTime("fecha"),
                            DocumentoCliente = reader.GetString("documento_cliente"),
                            NombreCliente = reader.IsDBNull(reader.GetOrdinal("nombre_cliente")) ? "" : reader.GetString("nombre_cliente"),
                            TotalBruto = reader.GetDecimal("total_bruto"),
                            TotalImpuesto = reader.GetDecimal("total_impuesto"),
                            TotalNeto = reader.GetDecimal("total_neto"),
                            IdUsuario = reader.IsDBNull(reader.GetOrdinal("idUsuario")) ? null : reader.GetInt32("idUsuario"),
                            Estado = reader.GetString("estado")
                        };
                    }
                }

                if (factura != null)
                {
                    const string sqlDetalles = @"
                        SELECT id, idFactura, idProducto, codigo, descripcion, cantidad, precio_unitario, subtotal
                        FROM factura_detalles
                        WHERE idFactura = @idFactura;";

                    using var cmdDet = new MySqlCommand(sqlDetalles, conn);
                    cmdDet.Parameters.AddWithValue("@idFactura", factura.Id);
                    using var rDet = cmdDet.ExecuteReader();
                    while (rDet.Read())
                    {
                        factura.Detalles.Add(new FacturaDetalleDto
                        {
                            Id = rDet.GetInt32("id"),
                            IdFactura = rDet.GetInt32("idFactura"),
                            IdProducto = rDet.GetInt32("idProducto"),
                            Codigo = rDet.GetString("codigo"),
                            Descripcion = rDet.GetString("descripcion"),
                            Cantidad = rDet.GetInt32("cantidad"),
                            PrecioUnitario = rDet.GetDecimal("precio_unitario"),
                            Subtotal = rDet.GetDecimal("subtotal")
                        });
                    }
                }

                return factura;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener factura: {ex.Message}");
                return null;
            }
        }
    }
}

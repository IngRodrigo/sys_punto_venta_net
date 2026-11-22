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
    public class CatalogosService : ICatalogosService
    {
        private readonly Database _database;

        public CatalogosService(Database database)
        {
            _database = database;
        }

        public List<CiudadDto> ObtenerCiudades()
        {
            var list = new List<CiudadDto>();
            try
            {
                using var conn = _database.CreateConnection();
                conn.Open();
                const string sql = "SELECT id, descripcion, idPais FROM ciudades ORDER BY descripcion ASC;";
                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new CiudadDto
                    {
                        Id = reader.GetInt32("id"),
                        Descripcion = reader.GetString("descripcion"),
                        IdPais = reader.GetInt32("idPais")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al listar ciudades: {ex.Message}");
            }
            return list;
        }

        public RespuestaOperacion GuardarCiudad(CiudadDto ciudad)
        {
            try
            {
                using var conn = _database.CreateConnection();
                conn.Open();
                if (ciudad.Id <= 0)
                {
                    const string sql = "INSERT INTO ciudades (descripcion, idPais) VALUES (@descripcion, @idPais); SELECT LAST_INSERT_ID();";
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@descripcion", ciudad.Descripcion);
                    cmd.Parameters.AddWithValue("@idPais", ciudad.IdPais <= 0 ? 1 : ciudad.IdPais);
                    var id = Convert.ToInt32(cmd.ExecuteScalar());
                    return new RespuestaOperacion { Exito = true, Mensaje = "Ciudad registrada", IdGenerado = id };
                }
                else
                {
                    const string sql = "UPDATE ciudades SET descripcion = @descripcion, idPais = @idPais WHERE id = @id;";
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id", ciudad.Id);
                    cmd.Parameters.AddWithValue("@descripcion", ciudad.Descripcion);
                    cmd.Parameters.AddWithValue("@idPais", ciudad.IdPais <= 0 ? 1 : ciudad.IdPais);
                    cmd.ExecuteNonQuery();
                    return new RespuestaOperacion { Exito = true, Mensaje = "Ciudad actualizada", IdGenerado = ciudad.Id };
                }
            }
            catch (Exception ex)
            {
                return new RespuestaOperacion { Exito = false, Mensaje = $"Error al guardar ciudad: {ex.Message}" };
            }
        }

        public List<PaisDto> ObtenerPaises()
        {
            var list = new List<PaisDto>();
            try
            {
                using var conn = _database.CreateConnection();
                conn.Open();
                const string sql = "SELECT id, descripcion, COALESCE(codigo_iso, '') AS iso FROM paises ORDER BY descripcion ASC;";
                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new PaisDto
                    {
                        Id = reader.GetInt32("id"),
                        Descripcion = reader.GetString("descripcion"),
                        CodigoIso = reader.GetString("iso")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al listar países: {ex.Message}");
            }
            return list;
        }

        public List<ImpuestoDto> ObtenerImpuestos()
        {
            var list = new List<ImpuestoDto>();
            try
            {
                using var conn = _database.CreateConnection();
                conn.Open();
                const string sql = "SELECT id, descripcion, porcentaje FROM impuestos ORDER BY id ASC;";
                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new ImpuestoDto
                    {
                        Id = reader.GetInt32("id"),
                        Descripcion = reader.GetString("descripcion"),
                        Porcentaje = reader.GetDecimal("porcentaje")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al listar impuestos: {ex.Message}");
            }
            return list;
        }

        public List<ProveedorDto> ObtenerProveedores()
        {
            var list = new List<ProveedorDto>();
            try
            {
                using var conn = _database.CreateConnection();
                conn.Open();
                const string sql = "SELECT id, razon_social, COALESCE(ruc, '') AS ruc, COALESCE(telefono, '') AS tel, COALESCE(direccion, '') AS dir, COALESCE(email, '') AS email FROM proveedores ORDER BY razon_social ASC;";
                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new ProveedorDto
                    {
                        Id = reader.GetInt32("id"),
                        RazonSocial = reader.GetString("razon_social"),
                        Ruc = reader.GetString("ruc"),
                        Telefono = reader.GetString("tel"),
                        Direccion = reader.GetString("dir"),
                        Email = reader.GetString("email")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al listar proveedores: {ex.Message}");
            }
            return list;
        }
    }
}

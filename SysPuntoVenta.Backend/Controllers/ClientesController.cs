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
    public class ClientesController : ControllerBase
    {
        private readonly Database _database;

        public ClientesController(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Obtiene la lista completa de clientes registrados con su país y ciudad asociados.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ClienteDto>>> GetAll()
        {
            var list = new List<ClienteDto>();
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string sql = @"
                    SELECT 
                        c.id, c.documento, c.nombre, c.telefono, c.direccion, c.email, 
                        c.idPais, c.idCiudad, c.create_at,
                        COALESCE(p.descripcion, '') AS paisNombre,
                        COALESCE(ci.descripcion, '') AS ciudadNombre
                    FROM clientes c
                    LEFT JOIN paises p ON c.idPais = p.id
                    LEFT JOIN ciudades ci ON c.idCiudad = ci.id
                    ORDER BY c.id DESC;";
                await using var cmd = new MySqlCommand(sql, conn);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new ClienteDto
                    {
                        Id = r.SafeGetInt32("id"),
                        Documento = r.SafeGetString("documento"),
                        Nombre = r.SafeGetString("nombre"),
                        Telefono = r.SafeGetString("telefono"),
                        Direccion = r.SafeGetString("direccion"),
                        Email = r.SafeGetString("email"),
                        IdPais = r.IsDBNull(r.GetOrdinal("idPais")) ? 1 : r.SafeGetInt32("idPais", 1),
                        PaisNombre = r.SafeGetString("paisNombre"),
                        IdCiudad = r.IsDBNull(r.GetOrdinal("idCiudad")) ? null : r.SafeGetInt32("idCiudad"),
                        CiudadNombre = r.SafeGetString("ciudadNombre"),
                        CreateAt = r.SafeGetDateTime("create_at")
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
        /// Obtiene un cliente por su ID numérico con detalle de su país y ciudad.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ClienteDto>> GetById(int id)
        {
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string sql = @"
                    SELECT 
                        c.id, c.documento, c.nombre, c.telefono, c.direccion, c.email, 
                        c.idPais, c.idCiudad, c.create_at,
                        COALESCE(p.descripcion, '') AS paisNombre,
                        COALESCE(ci.descripcion, '') AS ciudadNombre
                    FROM clientes c
                    LEFT JOIN paises p ON c.idPais = p.id
                    LEFT JOIN ciudades ci ON c.idCiudad = ci.id
                    WHERE c.id = @id LIMIT 1;";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    return Ok(new ClienteDto
                    {
                        Id = r.SafeGetInt32("id"),
                        Documento = r.SafeGetString("documento"),
                        Nombre = r.SafeGetString("nombre"),
                        Telefono = r.SafeGetString("telefono"),
                        Direccion = r.SafeGetString("direccion"),
                        Email = r.SafeGetString("email"),
                        IdPais = r.IsDBNull(r.GetOrdinal("idPais")) ? 1 : r.SafeGetInt32("idPais", 1),
                        PaisNombre = r.SafeGetString("paisNombre"),
                        IdCiudad = r.IsDBNull(r.GetOrdinal("idCiudad")) ? null : r.SafeGetInt32("idCiudad"),
                        CiudadNombre = r.SafeGetString("ciudadNombre"),
                        CreateAt = r.SafeGetDateTime("create_at")
                    });
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Busca clientes por coincidencia en documento o nombre, incluyendo información geográfica.
        /// </summary>
        [HttpGet("buscar")]
        public async Task<ActionResult<List<ClienteDto>>> Buscar([FromQuery] string q)
        {
            var list = new List<ClienteDto>();
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string sql = @"
                    SELECT 
                        c.id, c.documento, c.nombre, c.telefono, c.direccion, c.email, 
                        c.idPais, c.idCiudad, c.create_at,
                        COALESCE(p.descripcion, '') AS paisNombre,
                        COALESCE(ci.descripcion, '') AS ciudadNombre
                    FROM clientes c
                    LEFT JOIN paises p ON c.idPais = p.id
                    LEFT JOIN ciudades ci ON c.idCiudad = ci.id
                    WHERE c.documento LIKE @filtro OR c.nombre LIKE @filtro 
                    ORDER BY c.id DESC LIMIT 50;";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@filtro", $"%{q}%");
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new ClienteDto
                    {
                        Id = r.SafeGetInt32("id"),
                        Documento = r.SafeGetString("documento"),
                        Nombre = r.SafeGetString("nombre"),
                        Telefono = r.SafeGetString("telefono"),
                        Direccion = r.SafeGetString("direccion"),
                        Email = r.SafeGetString("email"),
                        IdPais = r.IsDBNull(r.GetOrdinal("idPais")) ? 1 : r.SafeGetInt32("idPais", 1),
                        PaisNombre = r.SafeGetString("paisNombre"),
                        IdCiudad = r.IsDBNull(r.GetOrdinal("idCiudad")) ? null : r.SafeGetInt32("idCiudad"),
                        CiudadNombre = r.SafeGetString("ciudadNombre"),
                        CreateAt = r.SafeGetDateTime("create_at")
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
        /// Crea un nuevo cliente o actualiza uno existente según su ID, guardando país y ciudad.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<RespuestaOperacion>> Save([FromBody] ClienteDto cliente)
        {
            if (string.IsNullOrWhiteSpace(cliente.Documento) || string.IsNullOrWhiteSpace(cliente.Nombre))
            {
                return BadRequest(new RespuestaOperacion { Exito = false, Mensaje = "El documento y nombre son obligatorios" });
            }

            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                if (cliente.Id <= 0)
                {
                    // Comprobar documento duplicado
                    const string checkSql = "SELECT COUNT(*) FROM clientes WHERE documento = @doc;";
                    await using (var checkCmd = new MySqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@doc", cliente.Documento.Trim());
                        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
                        {
                            return BadRequest(new RespuestaOperacion { Exito = false, Mensaje = "Ya existe un cliente con este documento o RUC" });
                        }
                    }

                    const string insertSql = @"
                        INSERT INTO clientes (documento, nombre, telefono, direccion, email, idPais, idCiudad, create_at) 
                        VALUES (@doc, @nom, @tel, @dir, @email, @idPais, @idCiudad, NOW());
                        SELECT LAST_INSERT_ID();";

                    await using var cmd = new MySqlCommand(insertSql, conn);
                    cmd.Parameters.AddWithValue("@doc", cliente.Documento.Trim());
                    cmd.Parameters.AddWithValue("@nom", cliente.Nombre.Trim());
                    cmd.Parameters.AddWithValue("@tel", cliente.Telefono ?? "");
                    cmd.Parameters.AddWithValue("@dir", cliente.Direccion ?? "");
                    cmd.Parameters.AddWithValue("@email", cliente.Email ?? "");
                    cmd.Parameters.AddWithValue("@idPais", (object?)cliente.IdPais ?? 1);
                    cmd.Parameters.AddWithValue("@idCiudad", (object?)cliente.IdCiudad ?? DBNull.Value);

                    var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return Ok(new RespuestaOperacion { Exito = true, Mensaje = "Cliente registrado con éxito", IdGenerado = newId });
                }
                else
                {
                    const string updateSql = @"
                        UPDATE clientes 
                        SET documento = @doc, nombre = @nom, telefono = @tel, direccion = @dir, 
                            email = @email, idPais = @idPais, idCiudad = @idCiudad 
                        WHERE id = @id;";

                    await using var cmd = new MySqlCommand(updateSql, conn);
                    cmd.Parameters.AddWithValue("@id", cliente.Id);
                    cmd.Parameters.AddWithValue("@doc", cliente.Documento.Trim());
                    cmd.Parameters.AddWithValue("@nom", cliente.Nombre.Trim());
                    cmd.Parameters.AddWithValue("@tel", cliente.Telefono ?? "");
                    cmd.Parameters.AddWithValue("@dir", cliente.Direccion ?? "");
                    cmd.Parameters.AddWithValue("@email", cliente.Email ?? "");
                    cmd.Parameters.AddWithValue("@idPais", (object?)cliente.IdPais ?? 1);
                    cmd.Parameters.AddWithValue("@idCiudad", (object?)cliente.IdCiudad ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                    return Ok(new RespuestaOperacion { Exito = true, Mensaje = "Cliente actualizado correctamente", IdGenerado = cliente.Id });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RespuestaOperacion { Exito = false, Mensaje = $"Error al guardar cliente: {ex.Message}" });
            }
        }

        /// <summary>
        /// Elimina un cliente por su identificador único.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<RespuestaOperacion>> Delete(int id)
        {
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string sql = "DELETE FROM clientes WHERE id = @id;";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();

                return Ok(new RespuestaOperacion { Exito = true, Mensaje = "Cliente eliminado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RespuestaOperacion { Exito = false, Mensaje = $"Error al eliminar cliente: {ex.Message}" });
            }
        }
    }
}

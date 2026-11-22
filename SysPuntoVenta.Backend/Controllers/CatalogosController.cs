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
    /// Controlador para la gestión y consulta de catálogos maestros del sistema
    /// (ciudades, países, impuestos y proveedores). Requiere autenticación Bearer.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CatalogosController : ControllerBase
    {
        private readonly Database _database;

        public CatalogosController(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Obtiene el listado completo de ciudades registradas ordenadas alfabéticamente.
        /// </summary>
        [HttpGet("ciudades")]
        public async Task<ActionResult<List<CiudadDto>>> GetCiudades()
        {
            var list = new List<CiudadDto>();
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();
                const string sql = "SELECT id, descripcion, idPais FROM ciudades ORDER BY descripcion ASC;";
                await using var cmd = new MySqlCommand(sql, conn);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new CiudadDto
                    {
                        Id = r.SafeGetInt32("id"),
                        Descripcion = r.SafeGetString("descripcion"),
                        IdPais = r.SafeGetInt32("idPais", 1)
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
        /// Registra una nueva ciudad o actualiza una existente en el catálogo.
        /// </summary>
        [HttpPost("ciudades")]
        public async Task<ActionResult<RespuestaOperacion>> SaveCiudad([FromBody] CiudadDto ciudad)
        {
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();
                if (ciudad.Id <= 0)
                {
                    const string sql = "INSERT INTO ciudades (descripcion, idPais) VALUES (@descripcion, @idPais); SELECT LAST_INSERT_ID();";
                    await using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@descripcion", ciudad.Descripcion);
                    cmd.Parameters.AddWithValue("@idPais", ciudad.IdPais <= 0 ? 1 : ciudad.IdPais);
                    var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return Ok(new RespuestaOperacion { Exito = true, Mensaje = "Ciudad registrada", IdGenerado = id });
                }
                else
                {
                    const string sql = "UPDATE ciudades SET descripcion = @descripcion, idPais = @idPais WHERE id = @id;";
                    await using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id", ciudad.Id);
                    cmd.Parameters.AddWithValue("@descripcion", ciudad.Descripcion);
                    cmd.Parameters.AddWithValue("@idPais", ciudad.IdPais <= 0 ? 1 : ciudad.IdPais);
                    await cmd.ExecuteNonQueryAsync();
                    return Ok(new RespuestaOperacion { Exito = true, Mensaje = "Ciudad actualizada", IdGenerado = ciudad.Id });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RespuestaOperacion { Exito = false, Mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene el listado de países disponibles en el sistema con su código ISO.
        /// </summary>
        [HttpGet("paises")]
        public async Task<ActionResult<List<PaisDto>>> GetPaises()
        {
            var list = new List<PaisDto>();
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();
                const string sql = "SELECT id, descripcion, COALESCE(codigo_iso, '') AS iso FROM paises ORDER BY descripcion ASC;";
                await using var cmd = new MySqlCommand(sql, conn);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new PaisDto
                    {
                        Id = r.SafeGetInt32("id"),
                        Descripcion = r.SafeGetString("descripcion"),
                        CodigoIso = r.SafeGetString("iso")
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
        /// Obtiene las tasas y categorías de impuestos configuradas (ej. IVA 10%).
        /// </summary>
        [HttpGet("impuestos")]
        public async Task<ActionResult<List<ImpuestoDto>>> GetImpuestos()
        {
            var list = new List<ImpuestoDto>();
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();
                const string sql = "SELECT id, descripcion, porcentaje FROM impuestos ORDER BY id ASC;";
                await using var cmd = new MySqlCommand(sql, conn);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new ImpuestoDto
                    {
                        Id = r.SafeGetInt32("id"),
                        Descripcion = r.SafeGetString("descripcion"),
                        Porcentaje = r.SafeGetDecimal("porcentaje", 10.0m)
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
        /// Obtiene la lista de proveedores disponibles para asociación con productos del inventario.
        /// </summary>
        [HttpGet("proveedores")]
        public async Task<ActionResult<List<ProveedorDto>>> GetProveedores()
        {
            var list = new List<ProveedorDto>();
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();
                const string sql = "SELECT id, razon_social, COALESCE(ruc, '') AS ruc, COALESCE(telefono, '') AS tel, COALESCE(direccion, '') AS dir, COALESCE(email, '') AS email FROM proveedores ORDER BY razon_social ASC;";
                await using var cmd = new MySqlCommand(sql, conn);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new ProveedorDto
                    {
                        Id = r.SafeGetInt32("id"),
                        RazonSocial = r.SafeGetString("razon_social"),
                        Ruc = r.SafeGetString("ruc"),
                        Telefono = r.SafeGetString("tel"),
                        Direccion = r.SafeGetString("dir"),
                        Email = r.SafeGetString("email")
                    });
                }
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }
    }
}

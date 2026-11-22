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
    public class AuthService : IAuthService
    {
        private readonly Database _database;

        public AuthService(Database database)
        {
            _database = database;
        }

        public LoginResponse Login(LoginRequest request)
        {
            try
            {
                using var conn = _database.CreateConnection();
                conn.Open();

                const string sql = @"
                    SELECT 
                        u.id AS idUsuario,
                        COALESCE(u.nombre, '') AS nombre,
                        COALESCE(u.apellido, '') AS apellido,
                        COALESCE(u.userName, '') AS user,
                        COALESCE(u.userPassword, '') AS password,
                        COALESCE(u.email, '') AS email,
                        COALESCE(u.edad, '') AS edad,
                        COALESCE(u.sexo, '') AS sexo,
                        COALESCE(u.direccion, '') AS direccion,
                        COALESCE(c.descripcion, '') AS ciudad,
                        COALESCE(p.descripcion, '') AS pais,
                        COALESCE(g.nombre, 'Usuario') AS rol,
                        u.create_at,
                        COALESCE(u.server, '') AS ip
                    FROM seg_usuarios u
                    LEFT JOIN seg_grupo_usuario gu ON u.id = gu.id_usuario
                    LEFT JOIN seg_grupos g ON gu.id_grupo = g.id
                    LEFT JOIN ciudades c ON u.idCiudad = c.id
                    LEFT JOIN paises p ON u.idPais = p.id
                    WHERE u.userName = @userName AND u.userPassword = @password
                    LIMIT 1;";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@userName", request.UserName);
                cmd.Parameters.AddWithValue("@password", request.Password);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var usuario = new UsuarioDto
                    {
                        Id = reader.SafeGetInt32("idUsuario"),
                        Nombre = reader.SafeGetString("nombre"),
                        Apellido = reader.SafeGetString("apellido"),
                        UserName = reader.SafeGetString("user"),
                        Email = reader.SafeGetString("email"),
                        Edad = reader.SafeGetString("edad"),
                        Sexo = reader.SafeGetString("sexo"),
                        Direccion = reader.SafeGetString("direccion"),
                        Ciudad = reader.SafeGetString("ciudad"),
                        Pais = reader.SafeGetString("pais"),
                        Rol = reader.SafeGetString("rol", "Usuario"),
                        CreateAt = reader.SafeGetDateTime("create_at"),
                        Ip = reader.SafeGetString("ip")
                    };

                    return new LoginResponse
                    {
                        Autenticado = true,
                        Mensaje = "Autenticación exitosa",
                        Usuario = usuario
                    };
                }

                return new LoginResponse
                {
                    Autenticado = false,
                    Mensaje = "Usuario o contraseña incorrectos"
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Autenticado = false,
                    Mensaje = $"Error al intentar autenticar: {ex.Message}"
                };
            }
        }

        public RespuestaOperacion RegistrarUsuario(RegistroUsuarioRequest request)
        {
            try
            {
                using var conn = _database.CreateConnection();
                conn.Open();

                const string checkSql = "SELECT COUNT(*) FROM seg_usuarios WHERE userName = @userName;";
                using (var checkCmd = new MySqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@userName", request.UserName);
                    var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        return new RespuestaOperacion { Exito = false, Mensaje = "El nombre de usuario ya está registrado" };
                    }
                }

                const string insertSql = @"
                    INSERT INTO seg_usuarios (nombre, apellido, userName, userPassword, email, idCiudad, idPais, create_at, server)
                    VALUES (@nombre, @apellido, @userName, @password, @email, @idCiudad, @idPais, NOW(), '127.0.0.1');
                    SELECT LAST_INSERT_ID();";

                using var cmd = new MySqlCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("@nombre", request.Nombre);
                cmd.Parameters.AddWithValue("@apellido", request.Apellido);
                cmd.Parameters.AddWithValue("@userName", request.UserName);
                cmd.Parameters.AddWithValue("@password", request.Password);
                cmd.Parameters.AddWithValue("@email", request.Email);
                cmd.Parameters.AddWithValue("@idCiudad", (object?)request.IdCiudad ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@idPais", (object?)request.IdPais ?? System.DBNull.Value);

                var newId = Convert.ToInt32(cmd.ExecuteScalar());

                // Asignar rol por defecto si existe la tabla seg_grupo_usuario
                try
                {
                    const string rolSql = "INSERT INTO seg_grupo_usuario (id_usuario, id_grupo) VALUES (@idUsuario, 2);";
                    using var cmdRol = new MySqlCommand(rolSql, conn);
                    cmdRol.Parameters.AddWithValue("@idUsuario", newId);
                    cmdRol.ExecuteNonQuery();
                }
                catch { }

                return new RespuestaOperacion
                {
                    Exito = true,
                    Mensaje = "Usuario registrado correctamente",
                    IdGenerado = newId
                };
            }
            catch (Exception ex)
            {
                return new RespuestaOperacion { Exito = false, Mensaje = $"Error al registrar usuario: {ex.Message}" };
            }
        }

        public List<UsuarioDto> ObtenerUsuarios()
        {
            var list = new List<UsuarioDto>();
            try
            {
                using var conn = _database.CreateConnection();
                conn.Open();

                const string sql = @"
                    SELECT 
                        u.id AS idUsuario, 
                        COALESCE(u.nombre, '') AS nombre, 
                        COALESCE(u.apellido, '') AS apellido, 
                        COALESCE(u.userName, '') AS user,
                        COALESCE(u.email, '') AS email, 
                        COALESCE(u.edad, '') AS edad, 
                        COALESCE(u.sexo, '') AS sexo, 
                        COALESCE(u.direccion, '') AS direccion,
                        COALESCE(c.descripcion, '') AS ciudad,
                        COALESCE(p.descripcion, '') AS pais,
                        COALESCE(g.nombre, 'Usuario') AS rol,
                        u.create_at, 
                        COALESCE(u.server, '') AS ip
                    FROM seg_usuarios u
                    LEFT JOIN seg_grupo_usuario gu ON u.id = gu.id_usuario
                    LEFT JOIN seg_grupos g ON gu.id_grupo = g.id
                    LEFT JOIN ciudades c ON u.idCiudad = c.id
                    LEFT JOIN paises p ON u.idPais = p.id
                    ORDER BY u.id ASC;";

                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new UsuarioDto
                    {
                        Id = reader.SafeGetInt32("idUsuario"),
                        Nombre = reader.SafeGetString("nombre"),
                        Apellido = reader.SafeGetString("apellido"),
                        UserName = reader.SafeGetString("user"),
                        Email = reader.SafeGetString("email"),
                        Ciudad = reader.SafeGetString("ciudad"),
                        Pais = reader.SafeGetString("pais"),
                        Rol = reader.SafeGetString("rol", "Usuario"),
                        CreateAt = reader.SafeGetDateTime("create_at"),
                        Ip = reader.SafeGetString("ip")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVISO] Error al obtener usuarios: {ex.Message}");
            }
            return list;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using SysPuntoVenta.Backend.Data;
using SysPuntoVenta.Backend.Services;
using SysPuntoVenta.Common.Models;

namespace SysPuntoVenta.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly Database _database;
        private readonly TokenService _tokenService;
        private readonly IEmailService _emailService;

        public AuthController(Database database, TokenService tokenService, IEmailService emailService)
        {
            _database = database;
            _tokenService = tokenService;
            _emailService = emailService;
        }

        /// <summary>
        /// Autentica al usuario mediante nombre de usuario y contraseña.
        /// Soporta hashing BCrypt y migración transparente de contraseñas previas en texto plano.
        /// Si la autenticación es exitosa, emite un Access Token (JWT Bearer) y un Refresh Token.
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

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
                        COALESCE(g.nombre, 'Vendedores') AS rol,
                        u.create_at,
                        COALESCE(u.server, '') AS ip
                    FROM seg_usuarios u
                    LEFT JOIN seg_grupo_usuario gu ON u.id = gu.id_usuario
                    LEFT JOIN seg_grupos g ON gu.id_grupo = g.id
                    LEFT JOIN ciudades c ON u.idCiudad = c.id
                    LEFT JOIN paises p ON u.idPais = p.id
                    WHERE u.userName = @userName
                    LIMIT 1;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@userName", request.UserName);

                UsuarioDto? usuario = null;
                string storedPassword = string.Empty;
                int userId = 0;

                await using (var r = await cmd.ExecuteReaderAsync())
                {
                    if (await r.ReadAsync())
                    {
                        userId = r.SafeGetInt32("idUsuario");
                        storedPassword = r.SafeGetString("password");
                        usuario = new UsuarioDto
                        {
                            Id = userId,
                            Nombre = r.SafeGetString("nombre"),
                            Apellido = r.SafeGetString("apellido"),
                            UserName = r.SafeGetString("user"),
                            Email = r.SafeGetString("email"),
                            Edad = r.SafeGetString("edad"),
                            Sexo = r.SafeGetString("sexo"),
                            Direccion = r.SafeGetString("direccion"),
                            Ciudad = r.SafeGetString("ciudad"),
                            Pais = r.SafeGetString("pais"),
                            Rol = r.SafeGetString("rol", "Vendedores"),
                            CreateAt = r.SafeGetDateTime("create_at"),
                            Ip = r.SafeGetString("ip")
                        };
                    }
                }

                if (usuario == null)
                {
                    return Unauthorized(new LoginResponse
                    {
                        Autenticado = false,
                        Mensaje = "Usuario o contraseña incorrectos."
                    });
                }

                // Validación de contraseña con BCrypt y soporte de migración transparente
                bool esValida = false;
                bool esHashBcrypt = storedPassword.StartsWith("$2a$") || storedPassword.StartsWith("$2b$") || storedPassword.StartsWith("$2y$");

                if (esHashBcrypt)
                {
                    esValida = BCrypt.Net.BCrypt.Verify(request.Password, storedPassword);
                }
                else
                {
                    // Migración transparente para cuentas en texto plano (ej. 'admin')
                    if (storedPassword == request.Password)
                    {
                        esValida = true;
                        try
                        {
                            string nuevoHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                            const string updateSql = "UPDATE seg_usuarios SET userPassword = @hash WHERE id = @id;";
                            await using var cmdUpdate = new MySqlCommand(updateSql, conn);
                            cmdUpdate.Parameters.AddWithValue("@hash", nuevoHash);
                            cmdUpdate.Parameters.AddWithValue("@id", userId);
                            await cmdUpdate.ExecuteNonQueryAsync();
                        }
                        catch { }
                    }
                }

                if (!esValida)
                {
                    return Unauthorized(new LoginResponse
                    {
                        Autenticado = false,
                        Mensaje = "Usuario o contraseña incorrectos."
                    });
                }

                // Generar tokens JWT
                var accessToken = _tokenService.GenerateAccessToken(usuario, out var exp);
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var (refreshToken, _) = await _tokenService.GenerateRefreshTokenAsync(usuario.Id, clientIp);

                return Ok(new LoginResponse
                {
                    Autenticado = true,
                    Mensaje = "Autenticación exitosa",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiraEl = exp,
                    Usuario = usuario
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new LoginResponse
                {
                    Autenticado = false,
                    Mensaje = $"Error al intentar autenticar: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Renueva el Access Token (JWT) utilizando un Refresh Token vigente.
        /// Aplica rotación de token: invalida el token actual y emite uno nuevo.
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult<RefreshTokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var resp = await _tokenService.RefreshTokenAsync(request.RefreshToken, clientIp);
                if (!resp.Exito)
                {
                    return Unauthorized(resp);
                }
                return Ok(resp);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RefreshTokenResponse { Exito = false, Mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Cierra la sesión activa revocando el Refresh Token provisto en el servidor.
        /// </summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            try
            {
                await _tokenService.RevokeTokenAsync(request.RefreshToken);
                return Ok(new { mensaje = "Sesión cerrada exitosamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Registra un nuevo operador en el sistema con contraseña cifrada mediante BCrypt.
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<RespuestaOperacion>> Register([FromBody] RegistroUsuarioRequest request)
        {
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string checkSql = "SELECT COUNT(*) FROM seg_usuarios WHERE userName = @userName;";
                await using (var checkCmd = new MySqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@userName", request.UserName);
                    if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
                    {
                        return BadRequest(new RespuestaOperacion { Exito = false, Mensaje = "El nombre de usuario ya está registrado." });
                    }
                }

                // Hasheo seguro de contraseña con BCrypt
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                const string insertSql = @"
                    INSERT INTO seg_usuarios (nombre, apellido, userName, userPassword, email, idCiudad, idPais, create_at, server)
                    VALUES (@nombre, @apellido, @userName, @password, @email, @idCiudad, @idPais, NOW(), '127.0.0.1');
                    SELECT LAST_INSERT_ID();";

                await using var cmd = new MySqlCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("@nombre", request.Nombre);
                cmd.Parameters.AddWithValue("@apellido", request.Apellido);
                cmd.Parameters.AddWithValue("@userName", request.UserName);
                cmd.Parameters.AddWithValue("@password", passwordHash);
                cmd.Parameters.AddWithValue("@email", request.Email);
                cmd.Parameters.AddWithValue("@idCiudad", (object?)request.IdCiudad ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@idPais", (object?)request.IdPais ?? DBNull.Value);

                var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                try
                {
                    const string rolSql = "INSERT INTO seg_grupo_usuario (id_usuario, id_grupo) VALUES (@idUsuario, 2);";
                    await using var cmdRol = new MySqlCommand(rolSql, conn);
                    cmdRol.Parameters.AddWithValue("@idUsuario", newId);
                    await cmdRol.ExecuteNonQueryAsync();
                }
                catch { }

                return Ok(new RespuestaOperacion
                {
                    Exito = true,
                    Mensaje = "Usuario registrado correctamente con contraseña cifrada.",
                    IdGenerado = newId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RespuestaOperacion { Exito = false, Mensaje = $"Error al registrar usuario: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtiene el listado completo de usuarios registrados en el sistema.
        /// Requiere autenticación Bearer Token.
        /// </summary>
        [Authorize]
        [HttpGet("usuarios")]
        public async Task<ActionResult<List<UsuarioDto>>> GetUsuarios()
        {
            var list = new List<UsuarioDto>();
            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string sql = @"
                    SELECT 
                        u.id AS idUsuario, 
                        COALESCE(u.nombre, '') AS nombre, 
                        COALESCE(u.apellido, '') AS apellido, 
                        COALESCE(u.userName, '') AS user,
                        COALESCE(u.email, '') AS email, 
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

                await using var cmd = new MySqlCommand(sql, conn);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new UsuarioDto
                    {
                        Id = r.SafeGetInt32("idUsuario"),
                        Nombre = r.SafeGetString("nombre"),
                        Apellido = r.SafeGetString("apellido"),
                        UserName = r.SafeGetString("user"),
                        Email = r.SafeGetString("email"),
                        Ciudad = r.SafeGetString("ciudad"),
                        Pais = r.SafeGetString("pais"),
                        Rol = r.SafeGetString("rol", "Usuario"),
                        CreateAt = r.SafeGetDateTime("create_at"),
                        Ip = r.SafeGetString("ip")
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
        /// Solicita un código OTP de recuperación de contraseña de 6 dígitos enviado por correo.
        /// Aplica una respuesta neutra para prevenir la enumeración de usuarios y cuentas.
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<ActionResult<RespuestaOperacion>> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EmailOUser))
            {
                return BadRequest(new RespuestaOperacion { Exito = false, Mensaje = "Debes ingresar tu correo o nombre de usuario." });
            }

            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string findSql = @"
                    SELECT id, nombre, email 
                    FROM seg_usuarios 
                    WHERE userName = @val OR email = @val 
                    LIMIT 1;";

                int idUsuario = 0;
                string nombre = string.Empty;
                string email = string.Empty;

                await using (var cmd = new MySqlCommand(findSql, conn))
                {
                    cmd.Parameters.AddWithValue("@val", request.EmailOUser.Trim());
                    await using var r = await cmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        idUsuario = r.SafeGetInt32("id");
                        nombre = r.SafeGetString("nombre");
                        email = r.SafeGetString("email");
                    }
                }

                // Si el usuario existe y tiene correo registrado, generar OTP de 6 dígitos
                if (idUsuario > 0 && !string.IsNullOrWhiteSpace(email))
                {
                    // Invalida solicitudes anteriores
                    const string invalidateSql = "UPDATE seg_password_resets SET utilizado = 1 WHERE id_usuario = @uid AND utilizado = 0;";
                    await using (var cmdInv = new MySqlCommand(invalidateSql, conn))
                    {
                        cmdInv.Parameters.AddWithValue("@uid", idUsuario);
                        await cmdInv.ExecuteNonQueryAsync();
                    }

                    // Genera código numérico seguro de 6 dígitos (100000 - 999999)
                    var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                    var expiraEl = DateTime.UtcNow.AddMinutes(15);
                    var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                    const string insertOtpSql = @"
                        INSERT INTO seg_password_resets (id_usuario, codigo_otp, expira_el, utilizado, creado_por_ip)
                        VALUES (@uid, @otp, @expira, 0, @ip);";

                    await using (var cmdInsert = new MySqlCommand(insertOtpSql, conn))
                    {
                        cmdInsert.Parameters.AddWithValue("@uid", idUsuario);
                        cmdInsert.Parameters.AddWithValue("@otp", otp);
                        cmdInsert.Parameters.AddWithValue("@expira", expiraEl);
                        cmdInsert.Parameters.AddWithValue("@ip", clientIp);
                        await cmdInsert.ExecuteNonQueryAsync();
                    }

                    // Enviar correo asíncrono
                    await _emailService.SendPasswordResetCodeAsync(email, nombre, otp);
                }

                // Respuesta neutra de seguridad
                return Ok(new RespuestaOperacion
                {
                    Exito = true,
                    Mensaje = "Si los datos ingresados corresponden a una cuenta registrada, hemos enviado un código de recuperación a su correo."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RespuestaOperacion { Exito = false, Mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Valida si un código OTP ingresado por el usuario es legítimo y no ha expirado ni sido utilizado.
        /// </summary>
        [HttpPost("verify-code")]
        public async Task<ActionResult<RespuestaOperacion>> VerifyResetCode([FromBody] VerifyCodeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EmailOUser) || string.IsNullOrWhiteSpace(request.CodigoOtp))
            {
                return BadRequest(new RespuestaOperacion { Exito = false, Mensaje = "El correo y código OTP son obligatorios." });
            }

            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string sql = @"
                    SELECT r.id
                    FROM seg_password_resets r
                    INNER JOIN seg_usuarios u ON r.id_usuario = u.id
                    WHERE (u.userName = @val OR u.email = @val)
                      AND r.codigo_otp = @otp
                      AND r.utilizado = 0
                      AND r.expira_el > UTC_TIMESTAMP()
                    LIMIT 1;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@val", request.EmailOUser.Trim());
                cmd.Parameters.AddWithValue("@otp", request.CodigoOtp.Trim());

                var idReset = await cmd.ExecuteScalarAsync();
                if (idReset != null)
                {
                    return Ok(new RespuestaOperacion { Exito = true, Mensaje = "Código verificado correctamente." });
                }

                return BadRequest(new RespuestaOperacion { Exito = false, Mensaje = "Código de recuperación inválido o expirado." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RespuestaOperacion { Exito = false, Mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Aplica la nueva contraseña cifrada con BCrypt al usuario, invalida el código OTP
        /// y revoca todos los Refresh Tokens para cerrar sesiones activas previas por seguridad.
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<ActionResult<RespuestaOperacion>> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EmailOUser) || 
                string.IsNullOrWhiteSpace(request.CodigoOtp) || 
                string.IsNullOrWhiteSpace(request.NuevaPassword))
            {
                return BadRequest(new RespuestaOperacion { Exito = false, Mensaje = "Todos los campos son obligatorios." });
            }

            try
            {
                await using var conn = _database.CreateConnection();
                await conn.OpenAsync();

                const string findSql = @"
                    SELECT r.id AS reset_id, u.id AS user_id, u.nombre, u.email
                    FROM seg_password_resets r
                    INNER JOIN seg_usuarios u ON r.id_usuario = u.id
                    WHERE (u.userName = @val OR u.email = @val)
                      AND r.codigo_otp = @otp
                      AND r.utilizado = 0
                      AND r.expira_el > UTC_TIMESTAMP()
                    LIMIT 1;";

                int resetId = 0;
                int userId = 0;
                string nombre = string.Empty;
                string email = string.Empty;

                await using (var cmd = new MySqlCommand(findSql, conn))
                {
                    cmd.Parameters.AddWithValue("@val", request.EmailOUser.Trim());
                    cmd.Parameters.AddWithValue("@otp", request.CodigoOtp.Trim());
                    await using var r = await cmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        resetId = r.SafeGetInt32("reset_id");
                        userId = r.SafeGetInt32("user_id");
                        nombre = r.SafeGetString("nombre");
                        email = r.SafeGetString("email");
                    }
                }

                if (resetId <= 0)
                {
                    return BadRequest(new RespuestaOperacion { Exito = false, Mensaje = "El código de recuperación es inválido o ha expirado." });
                }

                // 1. Hashea la nueva contraseña con BCrypt
                string newHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword);

                // 2. Actualiza contraseña en seg_usuarios
                const string updatePassSql = "UPDATE seg_usuarios SET userPassword = @hash WHERE id = @uid;";
                await using (var cmdPass = new MySqlCommand(updatePassSql, conn))
                {
                    cmdPass.Parameters.AddWithValue("@hash", newHash);
                    cmdPass.Parameters.AddWithValue("@uid", userId);
                    await cmdPass.ExecuteNonQueryAsync();
                }

                // 3. Marca el código como utilizado
                const string markOtpSql = "UPDATE seg_password_resets SET utilizado = 1 WHERE id = @rid;";
                await using (var cmdOtp = new MySqlCommand(markOtpSql, conn))
                {
                    cmdOtp.Parameters.AddWithValue("@rid", resetId);
                    await cmdOtp.ExecuteNonQueryAsync();
                }

                // 4. Revoca todos los Refresh Tokens del usuario (seguridad de cierre de sesiones previas)
                const string revokeTokensSql = "UPDATE seg_refresh_tokens SET revocado = 1 WHERE id_usuario = @uid;";
                await using (var cmdRevoke = new MySqlCommand(revokeTokensSql, conn))
                {
                    cmdRevoke.Parameters.AddWithValue("@uid", userId);
                    await cmdRevoke.ExecuteNonQueryAsync();
                }

                // 5. Envía notificación por correo
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                await _emailService.SendPasswordChangedNotificationAsync(email, nombre, clientIp);

                return Ok(new RespuestaOperacion
                {
                    Exito = true,
                    Mensaje = "Contraseña restablecida exitosamente. Ya puedes iniciar sesión con tu nueva contraseña."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RespuestaOperacion { Exito = false, Mensaje = ex.Message });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using SysPuntoVenta.Backend.Data;
using SysPuntoVenta.Common.Models;

namespace SysPuntoVenta.Backend.Services
{
    /// <summary>
    /// Servicio responsable de la generación, validación, rotación y revocación
    /// de tokens de autenticación JWT (Bearer) y Refresh Tokens para clientes desktop y móviles.
    /// </summary>
    public class TokenService
    {
        private readonly IConfiguration _config;
        private readonly Database _database;

        public TokenService(IConfiguration config, Database database)
        {
            _config = config;
            _database = database;
        }

        /// <summary>
        /// Genera un token JWT (Access Token) firmado criptográficamente con HMAC-SHA256,
        /// incluyendo los claims del usuario (ID, username, rol, email y token ID único).
        /// </summary>
        /// <param name="usuario">Datos del usuario autenticado.</param>
        /// <param name="expiration">Parámetro de salida con la fecha y hora de expiración en UTC.</param>
        /// <returns>Cadena en formato JWT lista para el encabezado Authorization Bearer.</returns>
        public string GenerateAccessToken(UsuarioDto usuario, out DateTime expiration)
        {
            var keyString = _config["Jwt:Key"] ?? "SysPuntoVenta_SuperSecretKey_2026_Net9_JWT_Token_Key!#*99";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var minutes = int.TryParse(_config["Jwt:AccessTokenExpirationMinutes"], out var m) ? m : 30;
            expiration = DateTime.UtcNow.AddMinutes(minutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new(ClaimTypes.Name, usuario.UserName),
                new(ClaimTypes.Role, string.IsNullOrWhiteSpace(usuario.Rol) ? "Vendedores" : usuario.Rol),
                new(JwtRegisteredClaimNames.Email, usuario.Email ?? ""),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "SysPuntoVenta.Backend",
                audience: _config["Jwt:Audience"] ?? "SysPuntoVenta.Clients",
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Genera un Refresh Token criptográfico seguro de 64 bytes (Base64),
        /// y lo persiste en la tabla 'seg_refresh_tokens' con su fecha de expiración e IP de origen.
        /// </summary>
        /// <param name="idUsuario">Identificador único del usuario en MySQL.</param>
        /// <param name="ipAddress">Dirección IP desde donde se emitió la solicitud.</param>
        /// <returns>Tupla con la cadena del token y su fecha de expiración.</returns>
        public async Task<(string token, DateTime expiration)> GenerateRefreshTokenAsync(int idUsuario, string ipAddress)
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var tokenString = Convert.ToBase64String(randomBytes);

            var days = int.TryParse(_config["Jwt:RefreshTokenExpirationDays"], out var d) ? d : 15;
            var expiration = DateTime.UtcNow.AddDays(days);

            await using var conn = _database.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO `seg_refresh_tokens` (`id_usuario`, `token`, `expira_el`, `revocado`, `creado_por_ip`)
                VALUES (@idUsuario, @token, @expiraEl, 0, @ip);";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
            cmd.Parameters.AddWithValue("@token", tokenString);
            cmd.Parameters.AddWithValue("@expiraEl", expiration);
            cmd.Parameters.AddWithValue("@ip", ipAddress ?? "127.0.0.1");
            await cmd.ExecuteNonQueryAsync();

            return (tokenString, expiration);
        }

        /// <summary>
        /// Valida un Refresh Token existente, revoca el token anterior por seguridad (Token Rotation)
        /// y emite un nuevo par de Access Token y Refresh Token sin requerir que el usuario reingrese contraseña.
        /// </summary>
        /// <param name="refreshToken">Cadena del refresh token recibido desde el cliente.</param>
        /// <param name="ipAddress">Dirección IP del cliente que solicita la renovación.</param>
        /// <returns>Objeto RefreshTokenResponse con los nuevos tokens o el motivo de fallo.</returns>
        public async Task<RefreshTokenResponse> RefreshTokenAsync(string refreshToken, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return new RefreshTokenResponse { Exito = false, Mensaje = "El refresh token es obligatorio." };
            }

            await using var conn = _database.CreateConnection();
            await conn.OpenAsync();

            const string selectSql = @"
                SELECT r.id, r.id_usuario, r.expira_el, r.revocado,
                       u.id AS uid, u.nombre, u.apellido, u.userName, u.email,
                       COALESCE(g.nombre, 'Vendedores') AS rol_nombre
                FROM `seg_refresh_tokens` r
                INNER JOIN `seg_usuarios` u ON r.id_usuario = u.id
                LEFT JOIN `seg_grupo_usuario` gu ON u.id = gu.id_usuario
                LEFT JOIN `seg_grupos` g ON gu.id_grupo = g.id
                WHERE r.token = @token
                LIMIT 1;";

            await using var cmd = new MySqlCommand(selectSql, conn);
            cmd.Parameters.AddWithValue("@token", refreshToken);

            int tokenRecordId = 0;
            int idUsuario = 0;
            DateTime expiraEl = DateTime.MinValue;
            bool revocado = true;
            UsuarioDto? user = null;

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    tokenRecordId = reader.GetInt32("id");
                    idUsuario = reader.GetInt32("id_usuario");
                    expiraEl = reader.GetDateTime("expira_el");
                    revocado = reader.GetBoolean("revocado");

                    user = new UsuarioDto
                    {
                        Id = reader.GetInt32("uid"),
                        Nombre = reader.SafeGetString("nombre"),
                        Apellido = reader.SafeGetString("apellido"),
                        UserName = reader.SafeGetString("userName"),
                        Email = reader.SafeGetString("email"),
                        Rol = reader.SafeGetString("rol_nombre")
                    };
                }
            }

            if (user == null || revocado || expiraEl < DateTime.UtcNow)
            {
                return new RefreshTokenResponse { Exito = false, Mensaje = "Token inválido, revocado o expirado." };
            }

            // Revocar token anterior para rotación de token
            const string revokeSql = "UPDATE `seg_refresh_tokens` SET `revocado` = 1 WHERE `id` = @id;";
            await using var cmdRevoke = new MySqlCommand(revokeSql, conn);
            cmdRevoke.Parameters.AddWithValue("@id", tokenRecordId);
            await cmdRevoke.ExecuteNonQueryAsync();

            // Generar nuevo Access Token y nuevo Refresh Token
            var newAccessToken = GenerateAccessToken(user, out var newAccessExp);
            var (newRefreshToken, _) = await GenerateRefreshTokenAsync(idUsuario, ipAddress);

            return new RefreshTokenResponse
            {
                Exito = true,
                Mensaje = "Token renovado exitosamente.",
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiraEl = newAccessExp
            };
        }

        /// <summary>
        /// Revoca explícitamente un Refresh Token para invalidar una sesión activa (ej. al hacer Logout).
        /// </summary>
        /// <param name="refreshToken">Cadena del token a invalidar.</param>
        /// <returns>True si el token fue localizado y revocado correctamente.</returns>
        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return false;

            await using var conn = _database.CreateConnection();
            await conn.OpenAsync();

            const string sql = "UPDATE `seg_refresh_tokens` SET `revocado` = 1 WHERE `token` = @token;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@token", refreshToken);
            var affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }
    }
}

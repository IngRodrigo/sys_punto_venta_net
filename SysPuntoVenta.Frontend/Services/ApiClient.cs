using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using SysPuntoVenta.Common.Models;

namespace SysPuntoVenta.Frontend.Services
{
    /// <summary>
    /// Cliente HTTP centralizado para la comunicación asíncrona con la API REST (.NET 9).
    /// Maneja automáticamente la inyección de Bearer Tokens JWT y la renovación transparente vía Refresh Token.
    /// </summary>
    public static class ApiClient
    {
        public static string BaseUrl { get; set; } = "http://localhost:5050/api";

        public static string? AccessToken { get; set; }
        public static string? RefreshToken { get; set; }

        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        /// <summary>
        /// Comprueba la conectividad con el servidor backend mediante un endpoint ligero no autenticado.
        /// </summary>
        /// <returns>True si el backend responde con código 200 OK.</returns>
        public static async Task<bool> CheckHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/dashboard/ping");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Autentica al usuario en el backend. En caso de éxito, guarda el Access Token y Refresh Token en memoria.
        /// </summary>
        /// <param name="username">Nombre de usuario del operador.</param>
        /// <param name="password">Contraseña en texto plano a verificar contra BCrypt.</param>
        /// <returns>Respuesta de autenticación con el usuario y los tokens emitidos.</returns>
        public static async Task<LoginResponse> LoginAsync(string username, string password)
        {
            try
            {
                var req = new LoginRequest { UserName = username, Password = password };
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/auth/login", req);
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (result != null && result.Autenticado)
                {
                    AccessToken = result.AccessToken;
                    RefreshToken = result.RefreshToken;
                    SetAuthHeader(result.AccessToken);
                }

                return result ?? new LoginResponse { Autenticado = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                return new LoginResponse { Autenticado = false, Mensaje = ex.Message };
            }
        }

        /// <summary>
        /// Inicia sesión o auto-registra al usuario mediante el flujo de autenticación de Google OAuth 2.0 / token ID.
        /// Soporta tanto el modo de desarrollo simulado como la validación de tokens reales en producción.
        /// </summary>
        /// <param name="req">Petición con el token ID o perfil simulado de Google.</param>
        /// <returns>Respuesta de login con el token JWT Bearer y datos del usuario autenticado.</returns>
        public static async Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest req)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/auth/google-login", req);
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (result != null && result.Autenticado)
                {
                    AccessToken = result.AccessToken;
                    RefreshToken = result.RefreshToken;
                    SetAuthHeader(result.AccessToken);
                }

                return result ?? new LoginResponse { Autenticado = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                return new LoginResponse { Autenticado = false, Mensaje = ex.Message };
            }
        }

        /// <summary>
        /// Renueva silenciosamente el Access Token utilizando el Refresh Token almacenado cuando se recibe un 401.
        /// </summary>
        /// <returns>True si la renovación fue exitosa y se actualizaron los tokens.</returns>
        public static async Task<bool> RenovarTokenAsync()
        {
            if (string.IsNullOrWhiteSpace(RefreshToken)) return false;

            try
            {
                var req = new RefreshTokenRequest { RefreshToken = RefreshToken };
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/auth/refresh", req);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
                    if (result != null && result.Exito)
                    {
                        AccessToken = result.AccessToken;
                        RefreshToken = result.RefreshToken;
                        SetAuthHeader(result.AccessToken);
                        return true;
                    }
                }
            }
            catch { }

            AccessToken = null;
            RefreshToken = null;
            SetAuthHeader(null);
            return false;
        }

        /// <summary>
        /// Notifica al backend la revocación del Refresh Token y limpia las credenciales locales de la sesión.
        /// </summary>
        public static async Task LogoutAsync()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(RefreshToken))
                {
                    await _httpClient.PostAsJsonAsync($"{BaseUrl}/auth/logout", new RefreshTokenRequest { RefreshToken = RefreshToken });
                }
            }
            catch { }
            finally
            {
                AccessToken = null;
                RefreshToken = null;
                SetAuthHeader(null);
                UserSession.CurrentUser = null;
            }
        }

        /// <summary>
        /// Configura el encabezado Authorization Bearer en las peticiones HTTP salientes.
        /// </summary>
        private static void SetAuthHeader(string? token)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        /// <summary>
        /// Realiza una solicitud HTTP GET autenticada, con reintento automático si el token expiró.
        /// </summary>
        private static async Task<T?> GetWithAuthAsync<T>(string url)
        {
            var response = await _httpClient.GetAsync(url);
            if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrWhiteSpace(RefreshToken))
            {
                var renewed = await RenovarTokenAsync();
                if (renewed)
                {
                    response = await _httpClient.GetAsync(url);
                }
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        /// <summary>
        /// Realiza una solicitud HTTP POST autenticada con cuerpo JSON, con reintento automático si el token expiró.
        /// </summary>
        private static async Task<HttpResponseMessage> PostWithAuthAsync<T>(string url, T data)
        {
            var response = await _httpClient.PostAsJsonAsync(url, data);
            if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrWhiteSpace(RefreshToken))
            {
                var renewed = await RenovarTokenAsync();
                if (renewed)
                {
                    response = await _httpClient.PostAsJsonAsync(url, data);
                }
            }
            return response;
        }

        /// <summary>
        /// Realiza una solicitud HTTP DELETE autenticada, con reintento automático si el token expiró.
        /// </summary>
        private static async Task<HttpResponseMessage> DeleteWithAuthAsync(string url)
        {
            var response = await _httpClient.DeleteAsync(url);
            if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrWhiteSpace(RefreshToken))
            {
                var renewed = await RenovarTokenAsync();
                if (renewed)
                {
                    response = await _httpClient.DeleteAsync(url);
                }
            }
            return response;
        }

        // --- DASHBOARD ---
        /// <summary>
        /// Recupera las métricas operativas del negocio (ventas, total recaudado, usuarios y clientes).
        /// </summary>
        public static async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var res = await GetWithAuthAsync<DashboardStatsDto>($"{BaseUrl}/dashboard/stats");
            return res ?? new DashboardStatsDto();
        }

        // --- USUARIOS ---
        /// <summary>
        /// Envía la solicitud para registrar un nuevo operador en el sistema con contraseña cifrada.
        /// </summary>
        public static async Task<RespuestaOperacion> RegistrarUsuarioAsync(RegistroUsuarioRequest req)
        {
            var response = await PostWithAuthAsync($"{BaseUrl}/auth/register", req);
            return await response.Content.ReadFromJsonAsync<RespuestaOperacion>() 
                ?? new RespuestaOperacion { Exito = false, Mensaje = "Error en el registro" };
        }

        /// <summary>
        /// Obtiene la lista de todos los usuarios registrados en el sistema.
        /// </summary>
        public static async Task<List<UsuarioDto>> GetUsuariosAsync()
        {
            var res = await GetWithAuthAsync<List<UsuarioDto>>($"{BaseUrl}/auth/usuarios");
            return res ?? new List<UsuarioDto>();
        }

        // --- CLIENTES ---
        /// <summary>
        /// Lista todos los clientes registrados en la cartera.
        /// </summary>
        public static async Task<List<ClienteDto>> GetClientesAsync()
        {
            var res = await GetWithAuthAsync<List<ClienteDto>>($"{BaseUrl}/clientes");
            return res ?? new List<ClienteDto>();
        }

        /// <summary>
        /// Busca clientes por coincidencia en número de documento o nombre/razón social.
        /// </summary>
        public static async Task<List<ClienteDto>> BuscarClientesAsync(string filtro)
        {
            var res = await GetWithAuthAsync<List<ClienteDto>>($"{BaseUrl}/clientes/buscar?q={Uri.EscapeDataString(filtro)}");
            return res ?? new List<ClienteDto>();
        }

        /// <summary>
        /// Crea o actualiza un cliente en la base de datos.
        /// </summary>
        public static async Task<RespuestaOperacion> GuardarClienteAsync(ClienteDto cliente)
        {
            var response = await PostWithAuthAsync($"{BaseUrl}/clientes", cliente);
            return await response.Content.ReadFromJsonAsync<RespuestaOperacion>() 
                ?? new RespuestaOperacion { Exito = false, Mensaje = "Error al guardar cliente" };
        }

        /// <summary>
        /// Elimina un cliente por su identificador único.
        /// </summary>
        public static async Task<RespuestaOperacion> EliminarClienteAsync(int id)
        {
            var response = await DeleteWithAuthAsync($"{BaseUrl}/clientes/{id}");
            return await response.Content.ReadFromJsonAsync<RespuestaOperacion>() 
                ?? new RespuestaOperacion { Exito = false, Mensaje = "Error al eliminar" };
        }

        // --- PRODUCTOS ---
        /// <summary>
        /// Lista todos los artículos del catálogo de productos.
        /// </summary>
        public static async Task<List<ProductoDto>> GetProductosAsync()
        {
            var res = await GetWithAuthAsync<List<ProductoDto>>($"{BaseUrl}/productos");
            return res ?? new List<ProductoDto>();
        }

        /// <summary>
        /// Obtiene los detalles de un producto específico por su ID.
        /// </summary>
        public static async Task<ProductoDto?> GetProductoPorIdAsync(int id)
        {
            return await GetWithAuthAsync<ProductoDto>($"{BaseUrl}/productos/{id}");
        }

        /// <summary>
        /// Busca productos en el catálogo filtrando por código o descripción.
        /// </summary>
        public static async Task<List<ProductoDto>> BuscarProductosAsync(string filtro)
        {
            var res = await GetWithAuthAsync<List<ProductoDto>>($"{BaseUrl}/productos/buscar?q={Uri.EscapeDataString(filtro)}");
            return res ?? new List<ProductoDto>();
        }

        /// <summary>
        /// Guarda un nuevo producto o actualiza sus precios, stock y datos de impuestos.
        /// </summary>
        public static async Task<RespuestaOperacion> GuardarProductoAsync(ProductoDto producto)
        {
            var response = await PostWithAuthAsync($"{BaseUrl}/productos", producto);
            return await response.Content.ReadFromJsonAsync<RespuestaOperacion>() 
                ?? new RespuestaOperacion { Exito = false, Mensaje = "Error al guardar producto" };
        }

        // --- FACTURACIÓN ---
        /// <summary>
        /// Registra una nueva factura/venta, calculando el IVA 10% y descontando el stock correspondiente.
        /// </summary>
        public static async Task<RespuestaOperacion> CrearFacturaAsync(FacturaDto factura)
        {
            var response = await PostWithAuthAsync($"{BaseUrl}/facturacion/crear", factura);
            return await response.Content.ReadFromJsonAsync<RespuestaOperacion>() 
                ?? new RespuestaOperacion { Exito = false, Mensaje = "Error al procesar venta" };
        }

        /// <summary>
        /// Obtiene el historial de comprobantes de ventas emitidos.
        /// </summary>
        public static async Task<List<FacturaDto>> GetFacturasAsync()
        {
            var res = await GetWithAuthAsync<List<FacturaDto>>($"{BaseUrl}/facturacion/listar");
            return res ?? new List<FacturaDto>();
        }

        // --- CATÁLOGOS ---
        /// <summary>
        /// Obtiene la lista de ciudades disponibles en el sistema.
        /// </summary>
        public static async Task<List<CiudadDto>> GetCiudadesAsync()
        {
            var res = await GetWithAuthAsync<List<CiudadDto>>($"{BaseUrl}/catalogos/ciudades");
            return res ?? new List<CiudadDto>();
        }

        /// <summary>
        /// Guarda o actualiza una ciudad en el catálogo.
        /// </summary>
        public static async Task<RespuestaOperacion> GuardarCiudadAsync(CiudadDto ciudad)
        {
            var response = await PostWithAuthAsync($"{BaseUrl}/catalogos/ciudades", ciudad);
            return await response.Content.ReadFromJsonAsync<RespuestaOperacion>() 
                ?? new RespuestaOperacion { Exito = false, Mensaje = "Error al guardar ciudad" };
        }

        /// <summary>
        /// Obtiene el listado de países registrados.
        /// </summary>
        public static async Task<List<PaisDto>> GetPaisesAsync()
        {
            var res = await GetWithAuthAsync<List<PaisDto>>($"{BaseUrl}/catalogos/paises");
            return res ?? new List<PaisDto>();
        }

        /// <summary>
        /// Obtiene las tasas de impuestos configuradas (ej. IVA 10%).
        /// </summary>
        public static async Task<List<ImpuestoDto>> GetImpuestosAsync()
        {
            var res = await GetWithAuthAsync<List<ImpuestoDto>>($"{BaseUrl}/catalogos/impuestos");
            return res ?? new List<ImpuestoDto>();
        }

        /// <summary>
        /// Obtiene el listado de proveedores disponibles para asignación a artículos.
        /// </summary>
        public static async Task<List<ProveedorDto>> GetProveedoresAsync()
        {
            var res = await GetWithAuthAsync<List<ProveedorDto>>($"{BaseUrl}/catalogos/proveedores");
            return res ?? new List<ProveedorDto>();
        }

        // --- SEGURIDAD: RECUPERACIÓN DE CONTRASEÑA ---
        /// <summary>
        /// Solicita el envío de un código OTP de 6 dígitos al correo del usuario para recuperación de contraseña.
        /// </summary>
        public static async Task<RespuestaOperacion> ForgotPasswordAsync(string emailOUser)
        {
            var req = new ForgotPasswordRequest { EmailOUser = emailOUser };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/auth/forgot-password", req);
            return await response.Content.ReadFromJsonAsync<RespuestaOperacion>()
                ?? new RespuestaOperacion { Exito = false, Mensaje = "Sin respuesta del servidor" };
        }

        /// <summary>
        /// Comprueba si el código OTP de 6 dígitos ingresado por el usuario es válido y se encuentra vigente.
        /// </summary>
        public static async Task<RespuestaOperacion> VerifyResetCodeAsync(string emailOUser, string codigoOtp)
        {
            var req = new VerifyCodeRequest { EmailOUser = emailOUser, CodigoOtp = codigoOtp };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/auth/verify-code", req);
            return await response.Content.ReadFromJsonAsync<RespuestaOperacion>()
                ?? new RespuestaOperacion { Exito = false, Mensaje = "Código inválido o expirado" };
        }

        /// <summary>
        /// Aplica la nueva contraseña en el sistema validando el código OTP correspondiente.
        /// </summary>
        public static async Task<RespuestaOperacion> ResetPasswordAsync(string emailOUser, string codigoOtp, string nuevaPassword)
        {
            var req = new ResetPasswordRequest { EmailOUser = emailOUser, CodigoOtp = codigoOtp, NuevaPassword = nuevaPassword };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/auth/reset-password", req);
            return await response.Content.ReadFromJsonAsync<RespuestaOperacion>()
                ?? new RespuestaOperacion { Exito = false, Mensaje = "Error al restablecer contraseña" };
        }

        // --- INFORMES ---
        /// <summary>
        /// Obtiene el informe de ventas filtrado por período ('dia', 'semana' o 'anio') con resumen de recaudación y grilla detallada.
        /// </summary>
        public static async Task<VentasPeriodoResumenDto> GetVentasPorPeriodoAsync(string periodo)
        {
            var res = await GetWithAuthAsync<VentasPeriodoResumenDto>($"{BaseUrl}/informes/ventas?periodo={periodo}");
            return res ?? new VentasPeriodoResumenDto();
        }
    }
}

using SysPuntoVenta.Common.Models;

namespace SysPuntoVenta.Frontend.Services
{
    public static class UserSession
    {
        public static UsuarioDto? CurrentUser { get; set; }
        public static bool IsAuthenticated => CurrentUser != null;
    }
}

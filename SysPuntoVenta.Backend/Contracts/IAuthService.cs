using CoreWCF;
using SysPuntoVenta.Common.Models;
using System.Collections.Generic;

namespace SysPuntoVenta.Backend.Contracts
{
    [ServiceContract(Namespace = "http://syspuntoventa.com/services/auth")]
    public interface IAuthService
    {
        [OperationContract]
        LoginResponse Login(LoginRequest request);

        [OperationContract]
        RespuestaOperacion RegistrarUsuario(RegistroUsuarioRequest request);

        [OperationContract]
        List<UsuarioDto> ObtenerUsuarios();
    }
}

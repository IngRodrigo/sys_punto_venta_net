using System;
using System.Collections.Generic;
using System.ServiceModel;
using SysPuntoVenta.Common.Models;

namespace SysPuntoVenta.Frontend.Services
{
    [ServiceContract(Name = "IAuthService", Namespace = "http://syspuntoventa.com/services/auth")]
    public interface IAuthServiceClient
    {
        [OperationContract]
        LoginResponse Login(LoginRequest request);

        [OperationContract]
        RespuestaOperacion RegistrarUsuario(RegistroUsuarioRequest request);

        [OperationContract]
        List<UsuarioDto> ObtenerUsuarios();
    }

    [ServiceContract(Name = "IProductoService", Namespace = "http://syspuntoventa.com/services/productos")]
    public interface IProductoServiceClient
    {
        [OperationContract]
        List<ProductoDto> ObtenerTodos();

        [OperationContract]
        ProductoDto? ObtenerPorId(int id);

        [OperationContract]
        ProductoDto? ObtenerPorCodigo(string codigo);

        [OperationContract]
        List<ProductoDto> Buscar(string filtro);

        [OperationContract]
        RespuestaOperacion GuardarProducto(ProductoDto producto);
    }

    [ServiceContract(Name = "IFacturacionService", Namespace = "http://syspuntoventa.com/services/facturacion")]
    public interface IFacturacionServiceClient
    {
        [OperationContract]
        RespuestaOperacion CrearFactura(FacturaDto factura);

        [OperationContract]
        List<FacturaDto> ObtenerFacturas();

        [OperationContract]
        FacturaDto? ObtenerFacturaPorId(int id);
    }

    [ServiceContract(Name = "ICatalogosService", Namespace = "http://syspuntoventa.com/services/catalogos")]
    public interface ICatalogosServiceClient
    {
        [OperationContract]
        List<CiudadDto> ObtenerCiudades();

        [OperationContract]
        RespuestaOperacion GuardarCiudad(CiudadDto ciudad);

        [OperationContract]
        List<PaisDto> ObtenerPaises();

        [OperationContract]
        List<ImpuestoDto> ObtenerImpuestos();

        [OperationContract]
        List<ProveedorDto> ObtenerProveedores();
    }
}

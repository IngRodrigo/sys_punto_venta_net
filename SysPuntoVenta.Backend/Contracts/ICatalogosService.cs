using CoreWCF;
using SysPuntoVenta.Common.Models;
using System.Collections.Generic;

namespace SysPuntoVenta.Backend.Contracts
{
    [ServiceContract(Namespace = "http://syspuntoventa.com/services/catalogos")]
    public interface ICatalogosService
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

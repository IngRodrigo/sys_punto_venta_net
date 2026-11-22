using CoreWCF;
using SysPuntoVenta.Common.Models;
using System.Collections.Generic;

namespace SysPuntoVenta.Backend.Contracts
{
    [ServiceContract(Namespace = "http://syspuntoventa.com/services/facturacion")]
    public interface IFacturacionService
    {
        [OperationContract]
        RespuestaOperacion CrearFactura(FacturaDto factura);

        [OperationContract]
        List<FacturaDto> ObtenerFacturas();

        [OperationContract]
        FacturaDto? ObtenerFacturaPorId(int id);
    }
}

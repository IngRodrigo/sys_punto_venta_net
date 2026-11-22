using CoreWCF;
using SysPuntoVenta.Common.Models;
using System.Collections.Generic;

namespace SysPuntoVenta.Backend.Contracts
{
    [ServiceContract(Namespace = "http://syspuntoventa.com/services/productos")]
    public interface IProductoService
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
}

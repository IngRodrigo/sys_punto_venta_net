using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SysPuntoVenta.Common.Models
{
    [DataContract]
    public class UsuarioDto
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public string Nombre { get; set; } = string.Empty;
        [DataMember] public string Apellido { get; set; } = string.Empty;
        [DataMember] public string UserName { get; set; } = string.Empty;
        [DataMember] public string Email { get; set; } = string.Empty;
        [DataMember] public string Edad { get; set; } = string.Empty;
        [DataMember] public string Sexo { get; set; } = string.Empty;
        [DataMember] public string Direccion { get; set; } = string.Empty;
        [DataMember] public int? IdCiudad { get; set; }
        [DataMember] public int? IdPais { get; set; }
        [DataMember] public string Ciudad { get; set; } = string.Empty;
        [DataMember] public string Pais { get; set; } = string.Empty;
        [DataMember] public string Rol { get; set; } = string.Empty;
        [DataMember] public DateTime? CreateAt { get; set; }
        [DataMember] public string Ip { get; set; } = string.Empty;
    }

    [DataContract]
    public class ClienteDto
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public string Documento { get; set; } = string.Empty;
        [DataMember] public string Nombre { get; set; } = string.Empty;
        [DataMember] public string Telefono { get; set; } = string.Empty;
        [DataMember] public string Direccion { get; set; } = string.Empty;
        [DataMember] public string Email { get; set; } = string.Empty;
        [DataMember] public DateTime? CreateAt { get; set; }
    }

    [DataContract]
    public class ProductoDto
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public string Codigo { get; set; } = string.Empty;
        [DataMember] public string Descripcion { get; set; } = string.Empty;
        [DataMember] public decimal Bruto { get; set; }
        [DataMember] public decimal PrecioImpuesto { get; set; }
        [DataMember] public decimal PrecioNeto { get; set; }
        [DataMember] public int IdImpuesto { get; set; } = 1;
        [DataMember] public int Cantidad { get; set; } = 100;
        [DataMember] public int IdProveedor { get; set; } = 1;
        [DataMember] public string ProveedorNombre { get; set; } = string.Empty;
        [DataMember] public string ImpuestoNombre { get; set; } = string.Empty;
        [DataMember] public DateTime? CreateAt { get; set; }
    }

    [DataContract]
    public class CiudadDto
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public string Descripcion { get; set; } = string.Empty;
        [DataMember] public int IdPais { get; set; }
    }

    [DataContract]
    public class PaisDto
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public string Descripcion { get; set; } = string.Empty;
        [DataMember] public string CodigoIso { get; set; } = string.Empty;
    }

    [DataContract]
    public class ProveedorDto
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public string RazonSocial { get; set; } = string.Empty;
        [DataMember] public string Ruc { get; set; } = string.Empty;
        [DataMember] public string Telefono { get; set; } = string.Empty;
        [DataMember] public string Direccion { get; set; } = string.Empty;
        [DataMember] public string Email { get; set; } = string.Empty;
    }

    [DataContract]
    public class ImpuestoDto
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public string Descripcion { get; set; } = string.Empty;
        [DataMember] public decimal Porcentaje { get; set; }
    }

    [DataContract]
    public class FacturaDto
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public string NumeroFactura { get; set; } = string.Empty;
        [DataMember] public DateTime Fecha { get; set; } = DateTime.Now;
        [DataMember] public string DocumentoCliente { get; set; } = string.Empty;
        [DataMember] public string NombreCliente { get; set; } = string.Empty;
        [DataMember] public decimal TotalBruto { get; set; }
        [DataMember] public decimal TotalImpuesto { get; set; }
        [DataMember] public decimal TotalNeto { get; set; }
        [DataMember] public int? IdUsuario { get; set; }
        [DataMember] public string Estado { get; set; } = "EMITIDA";
        [DataMember] public List<FacturaDetalleDto> Detalles { get; set; } = new();
    }

    [DataContract]
    public class FacturaDetalleDto
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public int IdFactura { get; set; }
        [DataMember] public int IdProducto { get; set; }
        [DataMember] public string Codigo { get; set; } = string.Empty;
        [DataMember] public string Descripcion { get; set; } = string.Empty;
        [DataMember] public int Cantidad { get; set; } = 1;
        [DataMember] public decimal PrecioUnitario { get; set; }
        [DataMember] public decimal Subtotal { get; set; }
    }

    [DataContract]
    public class DashboardStatsDto
    {
        [DataMember] public int CantidadVentas { get; set; }
        [DataMember] public decimal TotalRecaudado { get; set; }
        [DataMember] public int CantidadUsuarios { get; set; }
        [DataMember] public int CantidadClientes { get; set; }
        [DataMember] public int CantidadProductos { get; set; }
        [DataMember] public List<FacturaDto> VentasRecientes { get; set; } = new();
    }

    [DataContract]
    public class LoginRequest
    {
        [DataMember] public string UserName { get; set; } = string.Empty;
        [DataMember] public string Password { get; set; } = string.Empty;
    }

    [DataContract]
    public class LoginResponse
    {
        [DataMember] public bool Autenticado { get; set; }
        [DataMember] public string Mensaje { get; set; } = string.Empty;
        [DataMember] public string AccessToken { get; set; } = string.Empty;
        [DataMember] public string RefreshToken { get; set; } = string.Empty;
        [DataMember] public DateTime? ExpiraEl { get; set; }
        [DataMember] public UsuarioDto? Usuario { get; set; }
    }

    [DataContract]
    public class RefreshTokenRequest
    {
        [DataMember] public string RefreshToken { get; set; } = string.Empty;
    }

    [DataContract]
    public class RefreshTokenResponse
    {
        [DataMember] public bool Exito { get; set; }
        [DataMember] public string Mensaje { get; set; } = string.Empty;
        [DataMember] public string AccessToken { get; set; } = string.Empty;
        [DataMember] public string RefreshToken { get; set; } = string.Empty;
        [DataMember] public DateTime? ExpiraEl { get; set; }
    }

    [DataContract]
    public class RegistroUsuarioRequest
    {
        [DataMember] public string Nombre { get; set; } = string.Empty;
        [DataMember] public string Apellido { get; set; } = string.Empty;
        [DataMember] public string UserName { get; set; } = string.Empty;
        [DataMember] public string Password { get; set; } = string.Empty;
        [DataMember] public string Email { get; set; } = string.Empty;
        [DataMember] public int? IdCiudad { get; set; }
        [DataMember] public int? IdPais { get; set; }
    }

    [DataContract]
    public class RespuestaOperacion
    {
        [DataMember] public bool Exito { get; set; }
        [DataMember] public string Mensaje { get; set; } = string.Empty;
        [DataMember] public int IdGenerado { get; set; }
    }

    [DataContract]
    public class ForgotPasswordRequest
    {
        [DataMember] public string EmailOUser { get; set; } = string.Empty;
    }

    [DataContract]
    public class VerifyCodeRequest
    {
        [DataMember] public string EmailOUser { get; set; } = string.Empty;
        [DataMember] public string CodigoOtp { get; set; } = string.Empty;
    }

    [DataContract]
    public class ResetPasswordRequest
    {
        [DataMember] public string EmailOUser { get; set; } = string.Empty;
        [DataMember] public string CodigoOtp { get; set; } = string.Empty;
        [DataMember] public string NuevaPassword { get; set; } = string.Empty;
    }

    [DataContract]
    public class VentasPeriodoResumenDto
    {
        [DataMember] public string Periodo { get; set; } = string.Empty;
        [DataMember] public int TotalVentas { get; set; }
        [DataMember] public decimal TotalBruto { get; set; }
        [DataMember] public decimal TotalImpuesto { get; set; }
        [DataMember] public decimal TotalNeto { get; set; }
        [DataMember] public List<FacturaDto> Ventas { get; set; } = new();
    }
}

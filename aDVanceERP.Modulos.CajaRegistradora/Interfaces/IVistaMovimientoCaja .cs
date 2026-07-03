using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Modelos.Modulos.Comun;
using aDVanceERP.Core.Vistas.Comun.Interfaces;

namespace aDVanceERP.Modulos.CajaRegistradora.Interfaces {
    internal interface IVistaMovimientoCaja : IVistaRegistro {
        string Codigo { get; set; }
        long IdAlmacen { get; set; }
        string NombreAlmacen { get; set; }
        TipoMovimientoCajaEnum Tipo { get; set; }
        CanalPagoEnum CanalPago { get; set; }
        decimal Monto { get; set; }
        string? Descripcion { get; set; }
    }
}

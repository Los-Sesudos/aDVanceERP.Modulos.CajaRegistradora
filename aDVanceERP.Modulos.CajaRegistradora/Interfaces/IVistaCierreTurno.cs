using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Modelos.Modulos.Monedas;
using aDVanceERP.Core.Vistas.Comun.Interfaces;

namespace aDVanceERP.Modulos.CajaRegistradora.Interfaces {
    internal interface IVistaCierreTurno : IVistaRegistro {
        string CodigoTurno { get; set; }
        string NombreAlmacen { get; set; }
        DateTime FechaApertura { get; set; }
        decimal MontoApertura { get; set; }
        decimal TotalCalculado { get; set; }
        decimal TotalDeclarado { get; set; }
        decimal DiferenciaTotal { get; set; }

        string? Observaciones { get; set; }

        IEnumerable<CajaArqueo> ObtenerArqueo();
        void PopularDenominaciones(DenominacionMoneda[] denominaciones);
        List<CajaConciliacionMoneda> ObtenerConciliacion();
        void PopularConciliacion(List<TotalesCierreCaja> totalesCalculados, long idMonedaBase);

        event EventHandler? ArqueoModificado;
        event EventHandler? ConciliacionModificada;
        event EventHandler? ConfirmarCierreTurno;
    }
}

using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Vistas.Comun.Interfaces;

namespace aDVanceERP.Modulos.CajaRegistradora.Interfaces {
    internal interface IVistaCierreTurno : IVistaRegistro {
        string CodigoTurno { get; set; }
        string NombreAlmacen { get; set; }
        DateTime FechaApertura { get; set; }
        decimal MontoApertura { get; set; }
        decimal TotalEfectivoCalculado { get; set; }
        decimal TotalTransferenciasCalculado { get; set; }
        decimal MontoEfectivoDeclarado { get; set; }
        decimal MontoTransferenciasDeclarado { get; set; }
        decimal DiferenciaEfectivo { get; set; }
        decimal DiferenciaTransferencias { get; set; }

        string? Observaciones { get; set; }

        IEnumerable<CajaArqueo> ObtenerArqueo();
        void ActualizarTotalArqueo(decimal totalContado);

        event EventHandler? ArqueoModificado;
        event EventHandler? ConfirmarCierreTurno;
    }
}

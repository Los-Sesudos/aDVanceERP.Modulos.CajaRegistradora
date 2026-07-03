using aDVanceERP.Core.Eventos.Comun;
using aDVanceERP.Core.Eventos.Modulos.Caja;
using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Modelos.Comun;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Presentadores.Comun;
using aDVanceERP.Core.Repositorios.Modulos.Caja;
using aDVanceERP.Modulos.CajaRegistradora.Interfaces;

namespace aDVanceERP.Modulos.CajaRegistradora.Presentadores {
    internal class PresentadorCierreTurno : PresentadorVistaRegistro<IVistaCierreTurno, CajaTurno, RepoCajaTurno, FiltroBusquedaCajaTurno> {
        private CajaTurno _turno = null!;
        private TotalesCierreCaja? _totalesCalculados = null!;

        public PresentadorCierreTurno(IVistaCierreTurno vista) : base(vista) {
            vista.ArqueoModificado += OnArqueoModificado;
            vista.ConfirmarCierreTurno += OnConfirmarCierreTurno;

            AgregadorEventos.Suscribir<EventoMostrarVistaCierreTurno>(OnMostrarVistaCierreTurno);
        }

        private void OnMostrarVistaCierreTurno(EventoMostrarVistaCierreTurno e) {
            Vista.ModoEdicion = false;
            Vista.Restaurar();

            CargarDatosComunes(e.Turno, e.Totales);

            Vista.Mostrar();
        }

        private void CargarDatosComunes(CajaTurno turno, TotalesCierreCaja totales) {
            if (turno == null || turno.Estado != EstadoCajaTurnoEnum.Abierto) {
                CentroNotificaciones.MostrarNotificacion(
                    "El turno ya no está abierto.",
                    TipoNotificacionEnum.Advertencia);
                return;
            }

            _turno = turno;
            _totalesCalculados = totales;

            Vista.CodigoTurno = turno.Codigo;
            Vista.NombreAlmacen = turno.NombreAlmacen ?? string.Empty;
            Vista.FechaApertura = turno.FechaApertura;
            Vista.MontoApertura = turno.MontoApertura;
            Vista.TotalEfectivoCalculado = totales.TotalEfectivo;
            Vista.TotalTransferenciasCalculado = totales.TotalTransferencias;
            Vista.MontoEfectivoDeclarado = 0m;
            Vista.MontoTransferenciasDeclarado = 0m;
            Vista.DiferenciaEfectivo = 0m - totales.TotalEfectivo;
            Vista.DiferenciaTransferencias = 0m - totales.TotalTransferencias;

            Vista.ActualizarTotalArqueo(0m);
        }

        protected override CajaTurno? ObtenerEntidadDesdeVista() {
            return null;
        }

        protected override bool EntidadCorrecta() {
            if (_totalesCalculados == null) {
                CentroNotificaciones.MostrarNotificacion(
                    "Error interno: no se pudieron obtener los totales del turno.",
                    TipoNotificacionEnum.Error);
                return false;
            }

            return true;
        }

        protected void OnConfirmarCierreTurno(object? sender, EventArgs e) {
            AgregadorEventos.Publicar(new EventoConfirmacionCierreTurno() {
                Turno = _turno,
                ArqueoCaja = Vista.ObtenerArqueo().ToList(),
                TotalesCierreCaja = _totalesCalculados,
                MontoEfectivoDeclarado = Vista.MontoEfectivoDeclarado,
                MontoTransferenciasDeclarado = Vista.MontoTransferenciasDeclarado,
                DiferenciaEfectivo = Vista.MontoEfectivoDeclarado - _totalesCalculados?.TotalEfectivo ?? 0m,
                Observaciones = Vista.Observaciones
            });

            Vista.Cerrar();
            InvocarEntidadRegistradaActualizada();
        }

        private void OnArqueoModificado(object? sender, EventArgs e) {
            var arqueo = Vista.ObtenerArqueo();
            var totalContado = arqueo.Sum(a => a.Subtotal);

            Vista.ActualizarTotalArqueo(totalContado);
            Vista.MontoEfectivoDeclarado = totalContado;

            RecalcularDiferencias();
        }

        private void RecalcularDiferencias() {
            if (_totalesCalculados == null) 
                return;

            Vista.DiferenciaEfectivo = Vista.MontoEfectivoDeclarado - _totalesCalculados.TotalEfectivo;
            Vista.DiferenciaTransferencias = Vista.MontoTransferenciasDeclarado - _totalesCalculados.TotalTransferencias;
        }
    }
}

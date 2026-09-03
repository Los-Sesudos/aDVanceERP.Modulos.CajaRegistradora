using aDVanceERP.Core.Eventos.Comun;
using aDVanceERP.Core.Eventos.Modulos.Caja;
using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Modelos.Comun;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Presentadores.Comun;
using aDVanceERP.Core.Repositorios.Modulos.Caja;
using aDVanceERP.Core.Repositorios.Modulos.Monedas;
using aDVanceERP.Modulos.CajaRegistradora.Interfaces;

namespace aDVanceERP.Modulos.CajaRegistradora.Presentadores {
    internal class PresentadorCierreTurno : PresentadorVistaRegistro<IVistaCierreTurno, CajaTurno, RepoCajaTurno, FiltroBusquedaCajaTurno> {
        private CajaTurno _turno = null!;
        private List<TotalesCierreCaja>? _totalesCalculados = null!;

        public PresentadorCierreTurno(IVistaCierreTurno vista) : base(vista) {
            vista.ConciliacionModificada += OnConciliacionModificada;
            vista.ConfirmarCierreTurno += OnConfirmarCierreTurno;

            AgregadorEventos.Suscribir<EventoMostrarVistaCierreTurno>(OnMostrarVistaCierreTurno);
        }

        private void OnMostrarVistaCierreTurno(EventoMostrarVistaCierreTurno e) {
            Vista.ModoEdicion = false;
            Vista.Restaurar();

            CargarDatosComunes(e.Turno);

            Vista.Mostrar();
        }

        private void CargarDatosComunes(CajaTurno turno) {
            if (turno == null || turno.Estado != EstadoCajaTurnoEnum.Abierto) {
                CentroNotificaciones.MostrarNotificacion("El turno ya no está abierto.", TipoNotificacionEnum.Advertencia);
                return;
            }

            _turno = turno;
            var idMonedaBase = RepoMoneda.Instancia.ObtenerMonedaBase().Id;

            _totalesCalculados = RepoCajaMovimiento.Instancia.ObtenerTotalesPorCanalYMoneda(turno.Id);
            
            var idsMonedasUsadas = _totalesCalculados.Select(t => t.IdMoneda).ToHashSet();
            idsMonedasUsadas.Add(idMonedaBase);

            Vista.CodigoTurno = turno.Codigo;
            Vista.NombreAlmacen = turno.NombreAlmacen ?? string.Empty;
            Vista.FechaApertura = turno.FechaApertura;
            Vista.MontoApertura = turno.MontoApertura;

            var denominacionesTurno = RepoDenominacionMoneda.Instancia
                .ObtenerTodos()
                .Select(r => r.entidadBase)
                .Where(d => idsMonedasUsadas.Contains(d.IdMoneda))
                .ToArray();

            Vista.PopularDenominaciones(denominacionesTurno);
            Vista.PopularConciliacion(_totalesCalculados, idMonedaBase);
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
                Conciliacion = Vista.ObtenerConciliacion(),
                Observaciones = Vista.Observaciones
            });

            Vista.Cerrar();
            InvocarEntidadRegistradaActualizada();
        }

        private void OnConciliacionModificada(object? sender, EventArgs e) {
            // La diferencia por fila ya se recalculó en la propia tupla — aquí solo
            // se dispara para que el presentador sepa que hay cambios pendientes,
            // si necesitas validar algo antes de habilitar "Confirmar cierre".
        }

        //private void OnArqueoModificado(object? sender, EventArgs e) {
        //    var arqueo = Vista.ObtenerArqueo();
        //    var totalContado = arqueo.Sum(a => a.Subtotal);

        //    Vista.ActualizarTotalArqueo(totalContado);
        //    Vista.MontoEfectivoDeclarado = totalContado;

        //    RecalcularDiferencias();
        //}

        //private void RecalcularDiferencias() {
        //    if (_totalesCalculados == null) 
        //        return;

        //    Vista.DiferenciaEfectivo = Vista.MontoEfectivoDeclarado - _totalesCalculados.TotalEfectivo;
        //    Vista.DiferenciaTransferencias = Vista.MontoTransferenciasDeclarado - _totalesCalculados.TotalTransferencias;
        //}
    }
}

using aDVanceERP.Core.Eventos.Comun;
using aDVanceERP.Core.Eventos.Modulos.Caja;
using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Modelos.Comun;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Presentadores.Comun;
using aDVanceERP.Core.Repositorios.Modulos.Caja;
using aDVanceERP.Modulos.CajaRegistradora.Interfaces;

namespace aDVanceERP.Modulos.CajaRegistradora.Presentadores {
    internal class PresentadorTuplaTurno : PresentadorVistaTupla<IVistaTuplaTurno, CajaTurno> {
        public PresentadorTuplaTurno(IVistaTuplaTurno vista, CajaTurno entidad) : base(vista, entidad) {
            vista.VerDetalleTurno += OnVerDetalleTurno;
            vista.AnularTurno += OnAnularTurno;
        }

        private void OnVerDetalleTurno(object? sender, long e) {
            var turno = RepoCajaTurno.Instancia.ObtenerPorId(e);

            AgregadorEventos.Publicar(new EventoMostrarVistaDetalleTurno() { 
                Turno = turno!
            });
        }

        private void OnAnularTurno(object? sender, long e) {
            if (RepoCajaMovimiento.Instancia.TurnoTieneMovimientos(e)) {
                CentroNotificaciones.MostrarNotificacion(
                    "No es posible anular un turno que ya tiene movimientos registrados.",
                    TipoNotificacionEnum.Advertencia);
                return;
            }

            var anulado = RepoCajaTurno.Instancia.AnularTurno(e, "Anulado manualmente por el operador.");

            if (anulado) {
                Vista.Estado = EstadoCajaTurnoEnum.Anulado;

                CentroNotificaciones.MostrarNotificacion(
                    "El turno fue anulado correctamente.",
                    TipoNotificacionEnum.Ok);
            } else {
                CentroNotificaciones.MostrarNotificacion(
                    "No fue posible anular el turno. Verifique que siga abierto.",
                    TipoNotificacionEnum.Error);
            }
        }
    }
}

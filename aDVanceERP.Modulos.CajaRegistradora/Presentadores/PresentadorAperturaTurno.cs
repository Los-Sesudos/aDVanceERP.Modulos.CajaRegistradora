using aDVanceERP.Core.Eventos.Comun;
using aDVanceERP.Core.Eventos.Modulos.Caja;
using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Modelos.Comun;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Modelos.Modulos.Venta;
using aDVanceERP.Core.Presentadores.Comun;
using aDVanceERP.Core.Repositorios.Modulos.Caja;
using aDVanceERP.Core.Repositorios.Modulos.Inventario;
using aDVanceERP.Modulos.CajaRegistradora.Interfaces;

namespace aDVanceERP.Modulos.CajaRegistradora.Presentadores {
    internal class PresentadorAperturaTurno : PresentadorVistaRegistro<IVistaAperturaTurno, CajaTurno, RepoCajaTurno, FiltroBusquedaCajaTurno> {
        private Venta _venta = null!;
        
        public PresentadorAperturaTurno(IVistaAperturaTurno vista) : base(vista) {
            AgregadorEventos.Suscribir<EventoMostrarVistaAperturaTurno>(OnMostrarVistaAperturaTurno);
        }

        private void OnMostrarVistaAperturaTurno(EventoMostrarVistaAperturaTurno e) {
            Vista.ModoEdicion = false;
            Vista.Restaurar();

            CargarDatosComunes(e.Venta, e.IdAlmacen);

            Vista.Mostrar();
        }

        private void CargarDatosComunes(Venta venta, long idAlmacen) {
            _venta = venta;

            Vista.CargarAlmacenes([.. RepoAlmacen.Instancia.ObtenerTodos().Select(r => r.entidadBase)]);
        }

        protected override CajaTurno? ObtenerEntidadDesdeVista() {
            var idUsuario = ContextoSeguridad.UsuarioAutenticado?.Id ?? 0;
            var codigo = RepoCajaTurno.Instancia.GenerarCodigoTurno(Vista.Almacen?.Id ?? 0);

            var turno = new CajaTurno {
                Codigo = codigo,
                IdAlmacen = Vista.Almacen!.Id,
                IdCuentaApertura = idUsuario,
                FechaApertura = Vista.FechaApertura,
                MontoApertura = Vista.MontoApertura,
                Estado = EstadoCajaTurnoEnum.Abierto,
                ObservacionesApertura = Vista.Observaciones
            };

            return turno;
        }

        protected override void EventoPostAdicionEntidad(RepoCajaTurno repositorio, long id) {
            if (Vista.ModoEdicion)
                return;

            AgregadorEventos.Publicar(
                new EventoTurnoCajaAperturado() {
                    Turno = Entidad!,
                    Venta = _venta
                }
            );
        }

        protected override bool EntidadCorrecta() {
            if (Vista.MontoApertura < 0) {
                CentroNotificaciones.MostrarNotificacion(
                    "El monto de apertura no puede ser negativo.",
                    TipoNotificacionEnum.Advertencia);
                return false;
            }

            if (RepoCajaTurno.Instancia.ExisteTurnoAbierto(Vista.Almacen.Id)) {
                CentroNotificaciones.MostrarNotificacion(
                    "Ya existe un turno abierto para este almacén.",
                    TipoNotificacionEnum.Advertencia);
                return false;
            }

            return true;
        }

        public override void Dispose() {
            AgregadorEventos.Desuscribir<EventoMostrarVistaAperturaTurno>(OnMostrarVistaAperturaTurno);

            base.Dispose();
        }
    }
}

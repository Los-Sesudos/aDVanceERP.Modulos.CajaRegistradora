using aDVanceERP.Core.Eventos.Comun;
using aDVanceERP.Core.Eventos.Modulos.Caja;
using aDVanceERP.Core.Infraestructura.Extensiones.Comun;
using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Modelos.Comun;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Modelos.Modulos.Comun;
using aDVanceERP.Core.Modelos.Modulos.Inventario;
using aDVanceERP.Core.Presentadores.Comun;
using aDVanceERP.Core.Repositorios.Modulos.Caja;
using aDVanceERP.Core.Repositorios.Modulos.Monedas;
using aDVanceERP.Modulos.CajaRegistradora.Interfaces;

namespace aDVanceERP.Modulos.CajaRegistradora.Presentadores {
    internal class PresentadorMovimientoCaja : PresentadorVistaRegistro<IVistaMovimientoCaja, CajaMovimiento, RepoCajaMovimiento, FiltroBusquedaCajaMovimiento> {
        private long _idTurno;

        public PresentadorMovimientoCaja(IVistaMovimientoCaja vista) : base(vista) {
            AgregadorEventos.Suscribir<EventoMostrarVistaMovimientoCaja>(OnMostrarVistaMovimientoCaja);
        }

        private void OnMostrarVistaMovimientoCaja(EventoMostrarVistaMovimientoCaja e) {
            Vista.ModoEdicion = false;
            Vista.Restaurar();

            CargarDatosComunes(e.Turno!, e.Almacen);

            Vista.Mostrar();
        }

        private void CargarDatosComunes(CajaTurno turno, Almacen? almacen) {
            if (turno == null || turno.Estado != EstadoCajaTurnoEnum.Abierto) {
                CentroNotificaciones.MostrarNotificacion(
                    "No se pueden registrar movimientos en un turno cerrado o anulado.",
                    TipoNotificacionEnum.Advertencia);
                return;
            }

            _idTurno = turno.Id;

            var metodosPago = EnumExt.ObtenerNombresDescripciones<CanalPagoEnum>().Select(nd => nd.Nombre);

            Vista.Codigo = turno.Codigo;
            Vista.IdAlmacen = almacen!.Id;
            Vista.NombreAlmacen = almacen!.Nombre;
            Vista.CargarMetodosPago([.. metodosPago]);
            Vista.CargarMonedasPago([.. RepoMoneda.Instancia.ObtenerActivas()]);
            Vista.MonedaPago = RepoMoneda.Instancia.ObtenerMonedaBase();
        }

        protected override CajaMovimiento? ObtenerEntidadDesdeVista() {
            var idUsuario = ContextoSeguridad.UsuarioAutenticado?.Id ?? 0;

            // El monto es negativo para salidas
            var montoFinal = Vista.Tipo == TipoMovimientoCajaEnum.SalidaManual
                ? -Math.Abs(Vista.Monto)
                : Math.Abs(Vista.Monto);

            return new CajaMovimiento {
                IdTurno = _idTurno,
                Tipo = Vista.Tipo,
                CanalPago = Vista.CanalPago,
                IdMoneda = Vista.MonedaPago?.Id ?? 0,
                IdVenta = null,
                Monto = montoFinal,
                Descripcion = Vista.Descripcion,
                IdCuentaUsuario = idUsuario,
                FechaMovimiento = DateTime.Now
            };
        }

        protected override bool EntidadCorrecta() {
            if (Vista.Monto <= 0) {
                CentroNotificaciones.MostrarNotificacion(
                    "El monto debe ser mayor a cero.",
                    TipoNotificacionEnum.Advertencia);
                return false;
            }

            if (string.IsNullOrWhiteSpace(Vista.Descripcion)) {
                CentroNotificaciones.MostrarNotificacion(
                    "Ingrese una descripción para el movimiento.",
                    TipoNotificacionEnum.Advertencia);
                return false;
            }

            return true;
        }
    }
}

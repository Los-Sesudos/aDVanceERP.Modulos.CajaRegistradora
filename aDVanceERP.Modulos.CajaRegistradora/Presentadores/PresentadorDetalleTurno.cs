using aDVanceERP.Core.Eventos.Comun;
using aDVanceERP.Core.Eventos.Modulos.Caja;
using aDVanceERP.Core.Modelos.Comun.Interfaces;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Modelos.Modulos.Monedas;
using aDVanceERP.Core.Presentadores.Comun;
using aDVanceERP.Core.Repositorios.Modulos.Caja;
using aDVanceERP.Core.Repositorios.Modulos.Monedas;
using aDVanceERP.Core.Repositorios.Modulos.Seguridad;
using aDVanceERP.Core.Repositorios.Modulos.Venta;
using aDVanceERP.Modulos.CajaRegistradora.Interfaces;
using aDVanceERP.Modulos.CajaRegistradora.Vistas;

namespace aDVanceERP.Modulos.CajaRegistradora.Presentadores {
    internal class PresentadorDetalleTurno : PresentadorVistaGestion<PresentadorTuplaDetalleTurno, IVistaDetalleTurno, IVistaTuplaDetalleTurno, CajaMovimiento, RepoCajaMovimiento, FiltroBusquedaCajaMovimiento> {
        private CajaTurno _turno = null!;
        
        public PresentadorDetalleTurno(IVistaDetalleTurno vista) : base(vista) {
            AgregadorEventos.Suscribir<EventoMostrarVistaDetalleTurno>(OnMostrarVistaDetalleTurno);
        }

        private void OnMostrarVistaDetalleTurno(EventoMostrarVistaDetalleTurno e) {
            Vista.Restaurar();

            CargarDatosComunes(e.Turno);
           
            Vista.Mostrar();

            ActualizarResultadosBusqueda();
        }

        private void CargarDatosComunes(CajaTurno turno) {
            _turno = turno;

            Vista.CargarDatosGeneralesTurno(turno);
        }

        protected override PresentadorTuplaDetalleTurno ObtenerValoresTupla(CajaMovimiento entidad, List<IEntidadBaseDatos> entidadesExtra) {
            var presentadorTupla = new PresentadorTuplaDetalleTurno(new VistaTuplaDetalleTurno(), entidad);
            var venta = RepoVenta.Instancia.ObtenerPorId(entidad.IdVenta);
            var cuentaUsuario = RepoCuentaUsuario.Instancia.ObtenerPorId(entidad.IdCuentaUsuario);
            var moneda = RepoMoneda.Instancia.ObtenerPorId(entidad.IdMoneda);
            
            presentadorTupla.Vista.Id = entidad.Id;
            presentadorTupla.Vista.FechaMovimiento = entidad.FechaMovimiento.ToString("dd/MM/yyyy HH:mm");
            presentadorTupla.Vista.Tipo = entidad.Tipo;
            presentadorTupla.Vista.CanalPago = entidad.CanalPago;
            presentadorTupla.Vista.DescripcionFactura = entidad.IdVenta != null && entidad.IdVenta != 0
                ? venta!.NumeroFacturaTicket
                : entidad.Descripcion;
            presentadorTupla.Vista.Operador = cuentaUsuario?.Nombre ?? "admin";
            presentadorTupla.Vista.Moneda = moneda;
            presentadorTupla.Vista.Monto = entidad.Monto;

            return presentadorTupla;
        }

        public override void ActualizarResultadosBusqueda() {
            FiltroBusqueda = FiltroBusquedaCajaMovimiento.IdTurno;
            CriteriosBusqueda = [
                _turno.FechaApertura.ToString("yyyy-MM-dd HH:mm:ss"),
                _turno.FechaCierre?.ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.Today.ToString("yyyy-MM-dd 23:59:59"),
                _turno.Id.ToString()
            ];

            base.ActualizarResultadosBusqueda();
        }
    }
}

using aDVanceERP.Core.Eventos.Comun;
using aDVanceERP.Core.Eventos.Modulos;
using aDVanceERP.Core.Eventos.Modulos.Caja;
using aDVanceERP.Core.Presentadores.Comun;
using aDVanceERP.Core.Vistas.Comun.Interfaces;

namespace aDVanceERP.Modulos.CajaRegistradora.Presentadores {
    internal class PresentadorMenuCajaRegistradora : PresentadorVistaBase<IVistaMenu> {
        public PresentadorMenuCajaRegistradora(IVistaMenu vista) : base(vista) { 
            AgregadorEventos.Suscribir<EventoCambioMenu>(OnEventoCambioMenu);
            AgregadorEventos.Suscribir<EventoMostrarVistaMenuCajaRegistradora>(OnMostrarVistaMenuCaja);
        }

        private void OnEventoCambioMenu(EventoCambioMenu e) {
            Vista.Ocultar();
        }

        private void OnMostrarVistaMenuCaja(EventoMostrarVistaMenuCajaRegistradora e) {
            Vista.Restaurar();
            Vista.Mostrar();
            Vista.SeleccionarVistaInicial();
        }

        public override void Dispose() {
            AgregadorEventos.Desuscribir<EventoCambioMenu>(OnEventoCambioMenu);
            AgregadorEventos.Desuscribir<EventoMostrarVistaMenuCajaRegistradora>(OnMostrarVistaMenuCaja);
        }
    }
}

using aDVanceERP.Core.Eventos.Comun;
using aDVanceERP.Core.Eventos.Modulos;
using aDVanceERP.Core.Eventos.Modulos.Caja;
using aDVanceERP.Core.Eventos.Modulos.Seguridad;
using aDVanceERP.Core.Eventos.Modulos.Venta;
using aDVanceERP.Core.Extension.Interfaces.BaseConcreta;
using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Modelos.Modulos.Comun;
using aDVanceERP.Core.Presentadores.Comun.Interfaces;
using aDVanceERP.Core.Vistas.Comun.Interfaces;
using aDVanceERP.Modulos.CajaRegistradora.Manejadores;
using aDVanceERP.Modulos.CajaRegistradora.Presentadores;
using aDVanceERP.Modulos.CajaRegistradora.Properties;
using aDVanceERP.Modulos.CajaRegistradora.Vistas;

using Guna.UI2.WinForms;

namespace aDVanceERP.Modulos.CajaRegistradora {
    public class ModuloCaja : ModuloExtensionBase {
        private Guna2CircleButton _btnAccesoModulo = new Guna2CircleButton();
        private PresentadorMenuCajaRegistradora _menuCajaRegistradora = null!;
        private PresentadorGestionCaja _cajaTurno = null!;
        private PresentadorAperturaTurno _aperturaTurno = null!;
        private PresentadorCierreTurno _cierreTurno = null!;
        private PresentadorMovimientoCaja _movimientoCaja = null!;
        private PresentadorDetalleTurno _detalleTurno = null!;

        // Manejadores de eventos
        private ManejadorCaja _manejadorCaja = null!;
        private ManejadorAperturaTurno _manejadorAperturaTurno = null!;
        private ManejadorCierreTurno _manejadorCierreTurno = null!;

        public ModuloCaja() {
            Nombre = ModuloSistemaEnum.MOD_CAJA_REGISTRADORA;
            Version = new Version(1, 0, 0, 0);
        }

        public override void Inicializar(IPresentadorVistaPrincipal<IVistaPrincipal> principal) {
            // Botón de acceso al módulo
            _btnAccesoModulo.Name = "btnAccesoModuloCaja";
            _btnAccesoModulo.CustomImages.Image = Resources.cash_registerB_24px;
            _btnAccesoModulo.CustomImages.ImageOffset = new Point(0, 2);
            _btnAccesoModulo.TabIndex = 5;
            _btnAccesoModulo.Click += delegate {
                AgregadorEventos.Publicar(new EventoCambioModulo());
                AgregadorEventos.Publicar(new EventoCambioMenu());
                AgregadorEventos.Publicar(new EventoMostrarVistaMenuCajaRegistradora());
            };

            // Menu
            _menuCajaRegistradora = new PresentadorMenuCajaRegistradora(new VistaMenuCaja());

            // Contenedor de módulos
            // Caja - Turno
            _cajaTurno = new PresentadorGestionCaja(new VistaGestionCajaTurno());
            _aperturaTurno = new PresentadorAperturaTurno(new VistaAperturaTurno());
            _cierreTurno = new PresentadorCierreTurno(new VistaCierreTurno());
            _movimientoCaja = new PresentadorMovimientoCaja(new VistaMovimientoCaja());
            _aperturaTurno.EntidadRegistradaActualizada += (s, e) => _cajaTurno.ActualizarResultadosBusqueda();
            _cierreTurno.EntidadRegistradaActualizada += (s, e) => _cajaTurno.ActualizarResultadosBusqueda();
            _movimientoCaja.EntidadRegistradaActualizada += (s, e) => _cajaTurno.ActualizarResultadosBusqueda();

            // Detalle - Turno
            _detalleTurno = new PresentadorDetalleTurno(new VistaDetalleTurno());

            // Manejadores de eventos
            _manejadorCaja = new ManejadorCaja();
            _manejadorAperturaTurno = new ManejadorAperturaTurno();
            _manejadorCierreTurno = new ManejadorCierreTurno();

            base.Inicializar(principal);
        }

        protected override void InicializarVistas() {
            // Agregar botón de acceso al módulo
            _principal.Modulos.AdicionarBotonAccesoModulo(_btnAccesoModulo, "Caja registradora");

            // Agregar menú del módulo
            _principal.Vista.BarraTitulo.Registrar(_menuCajaRegistradora.Vista);

            // Contenedor de módulos
            // Caja - Turno
            _principal.Modulos.Vista.PanelCentral.Registrar(_cajaTurno.Vista);
            _principal.Modulos.Vista.PanelCentral.Registrar(_aperturaTurno.Vista);
            _principal.Modulos.Vista.PanelCentral.Registrar(_cierreTurno.Vista);
            _principal.Modulos.Vista.PanelCentral.Registrar(_movimientoCaja.Vista);

            // Detalle - Turno
            _principal.Modulos.Vista.PanelCentral.Registrar(_detalleTurno.Vista);
        }

        protected override void InicializarEventos() {
            AgregadorEventos.Suscribir<EventoPagoVentaConfirmado>(_manejadorCaja.Manejar);
            AgregadorEventos.Suscribir<EventoPagoVentaAnulado>(_manejadorCaja.Manejar);
            AgregadorEventos.Suscribir<EventoTurnoCajaAperturado>(_manejadorAperturaTurno.Manejar);
            AgregadorEventos.Suscribir<EventoConfirmacionCierreTurno>(_manejadorCierreTurno.Manejar);
            AgregadorEventos.Suscribir<EventoUsuarioAutenticado>(OnUsuarioAutenticado);
        }

        private void OnUsuarioAutenticado(EventoUsuarioAutenticado e) {
            _btnAccesoModulo.Visible = ContextoSeguridad.TieneAccesoModulo(ModuloSistemaEnum.MOD_CAJA_REGISTRADORA);
        }

        public override void Apagar() {
            // Desuscribir eventos
            AgregadorEventos.Desuscribir<EventoPagoVentaConfirmado>(_manejadorCaja.Manejar);
            AgregadorEventos.Desuscribir<EventoPagoVentaAnulado>(_manejadorCaja.Manejar);
            AgregadorEventos.Desuscribir<EventoTurnoCajaAperturado>(_manejadorAperturaTurno.Manejar);
            AgregadorEventos.Desuscribir<EventoConfirmacionCierreTurno>(_manejadorCierreTurno.Manejar);
            AgregadorEventos.Desuscribir<EventoUsuarioAutenticado>(OnUsuarioAutenticado);

            _menuCajaRegistradora.Dispose();
            _cajaTurno.Dispose();
            _aperturaTurno.Dispose();
            _cierreTurno.Dispose();
            _movimientoCaja.Dispose();
            _detalleTurno.Dispose();
        }
    }
}

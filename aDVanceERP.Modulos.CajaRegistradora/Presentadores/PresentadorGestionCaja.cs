using aDVanceERP.Core.Eventos;
using aDVanceERP.Core.Eventos.Comun;
using aDVanceERP.Core.Eventos.Modulos.Caja;
using aDVanceERP.Core.Infraestructura.Extensiones.Comun;
using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Modelos.Comun;
using aDVanceERP.Core.Modelos.Comun.Interfaces;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Presentadores.Comun;
using aDVanceERP.Core.Repositorios.Modulos.Caja;
using aDVanceERP.Core.Repositorios.Modulos.Inventario;
using aDVanceERP.Modulos.CajaRegistradora.Documentos;
using aDVanceERP.Modulos.CajaRegistradora.Interfaces;
using aDVanceERP.Modulos.CajaRegistradora.Vistas;

namespace aDVanceERP.Modulos.CajaRegistradora.Presentadores {
    internal class PresentadorGestionCaja : PresentadorVistaGestion<PresentadorTuplaTurno, IVistaGestionCaja, IVistaTuplaTurno, CajaTurno, RepoCajaTurno, FiltroBusquedaCajaTurno> {
        public PresentadorGestionCaja(IVistaGestionCaja vista) : base(vista) {
            vista.AbrirTurno += OnAbrirTurno;
            vista.GenerarDocumentoResumenMovimientos += OnGenerarDocumentoResumenMovimientos;
            vista.CerrarTurno += OnCerrarTurno;
            vista.RegistrarMovimiento += OnRegistrarMovimiento;
            
            AgregadorEventos.Suscribir<EventoMostrarVistaGestionCaja>(OnMostrarVistaGestionCaja);
        }

        private void OnMostrarVistaGestionCaja(EventoMostrarVistaGestionCaja e) {
            CargarDatosComunes();

            Vista.Restaurar();
            Vista.Mostrar();

            ActualizarResultadosBusqueda();
        }

        private void CargarDatosComunes() {
            Vista.CargarFiltroAlmacenes([.. RepoAlmacen.Instancia.ObtenerTodos().Select(resultado => resultado.entidadBase)]);
            Vista.CargarFiltrosBusqueda([.. EnumExt.ObtenerNombresDescripciones<FiltroBusquedaCajaTurno>()]);
        }

        private void OnAbrirTurno(object? sender, EventArgs e) {
            if (Vista.IdAlmacenSeleccionado <= 0) {
                CentroNotificaciones.MostrarNotificacion(
                    "Seleccione un almacén antes de abrir un turno de caja.",
                    TipoNotificacionEnum.Advertencia);
                return;
            }

            if (RepoCajaTurno.Instancia.ExisteTurnoAbierto(Vista.IdAlmacenSeleccionado)) {
                CentroNotificaciones.MostrarNotificacion(
                    "Ya existe un turno abierto para este almacén. Ciérrelo antes de abrir uno nuevo.",
                    TipoNotificacionEnum.Advertencia);
                return;
            }

            AgregadorEventos.Publicar(new EventoMostrarVistaAperturaTurno() { 
                IdAlmacen = Vista.IdAlmacenSeleccionado
            });
        }

        private void OnGenerarDocumentoResumenMovimientos(object? sender, DateTime fecha) {
            var resumen = new DocCierreCaja(fecha);

            resumen.GenerarDocumento(mostrar: true);
        }

        private void OnCerrarTurno(object? sender, EventArgs e) {
            AgregadorEventos.Publicar(new EventoMostrarVistaCierreTurno() { 
                CodigoTurno = Vista.CodigoTurnoActivo,
                IdAlmacen = Vista.IdAlmacenSeleccionado
            });
        }

        private void OnRegistrarMovimiento(object? sender, EventArgs e) {
            AgregadorEventos.Publicar(new EventoMostrarVistaMovimientoCaja() {
                CodigoTurno = Vista.CodigoTurnoActivo,
                IdAlmacen = Vista.IdAlmacenSeleccionado
            });
        }

        protected override PresentadorTuplaTurno ObtenerValoresTupla(CajaTurno entidad, List<IEntidadBaseDatos> entidadesExtra) {
            var presentadorTupla = new PresentadorTuplaTurno(new VistaTuplaTurno(), entidad);

            presentadorTupla.Vista.Id = entidad.Id;
            presentadorTupla.Vista.Codigo = entidad.Codigo;
            presentadorTupla.Vista.NombreAlmacen = entidad.NombreAlmacen ?? "-";
            presentadorTupla.Vista.NombreUsuarioApertura = entidad.NombreUsuarioApertura ?? "-";
            presentadorTupla.Vista.FechaApertura = entidad.FechaApertura;
            presentadorTupla.Vista.FechaCierre = entidad.FechaCierre;
            presentadorTupla.Vista.MontoApertura = entidad.MontoApertura;
            presentadorTupla.Vista.MontoEfectivoCalculado = entidad.MontoEfectivoCalculado;
            presentadorTupla.Vista.MontoEfectivoDeclarado = entidad.MontoEfectivoDeclarado;
            presentadorTupla.Vista.DiferenciaEfectivo = entidad.DiferenciaEfectivo;
            presentadorTupla.Vista.MontoTransferenciasCalculado = entidad.MontoTransferenciasCalculado;
            presentadorTupla.Vista.MontoTransferenciasDeclarado = entidad.MontoTransferenciasDeclarado;
            presentadorTupla.Vista.DiferenciaTransferencias = entidad.DiferenciaTransferencias;
            presentadorTupla.Vista.Estado = entidad.Estado;

            return presentadorTupla;
        }

        public override void ActualizarResultadosBusqueda() {
            if (FiltroBusqueda == FiltroBusquedaCajaTurno.Todos &&
               (CriteriosBusqueda == null || CriteriosBusqueda.Length == 0)) {
                CriteriosBusqueda = [
                    "0",
                    (new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).ToString("yyyy-MM-dd 00:00:00"),
                    DateTime.Today.ToString("yyyy-MM-dd 23:59:59"),
                    string.Empty
                ];
            }

            base.ActualizarResultadosBusqueda();

            Vista.RefrescarEstadoTurnoActivo();
        }

        public override void Dispose() {
            AgregadorEventos.Desuscribir<EventoMostrarVistaGestionCaja>(OnMostrarVistaGestionCaja);

            base.Dispose();
        }
    }
}

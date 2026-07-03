using aDVanceERP.Core.Eventos.Comun;
using aDVanceERP.Core.Eventos.Modulos.Caja;
using aDVanceERP.Core.Eventos.Modulos.Venta;
using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Modelos.Comun;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Repositorios.Modulos.Caja;
using aDVanceERP.Core.Repositorios.Modulos.Venta;

namespace aDVanceERP.Modulos.CajaRegistradora.Manejadores {
    internal class ManejadorCaja {
        private readonly RepoCajaTurno _repoCajaTurno = null!;
        private readonly RepoCajaMovimiento _repoCajaMovimiento = null!;

        internal ManejadorCaja() {
            _repoCajaTurno = RepoCajaTurno.Instancia;
            _repoCajaMovimiento = RepoCajaMovimiento.Instancia;
        }

        internal void Manejar(EventoPagoVentaConfirmado e) {
            if (e.Pago == null || !ContextoSeguridad.EstaAutenticado)
                return;

            // Buscar el turno activo del almacén donde se realizó la venta
            var venta = RepoVenta.Instancia.ObtenerPorId(e.Pago.IdVenta);
            var idAlmacen = venta!.IdAlmacenOrigen;
            var turnoActivo = _repoCajaTurno.ObtenerTurnoAbierto(idAlmacen);

            if (turnoActivo == null) {
                // No hay turno abierto, no se puede registrar el movimiento.
                // Proponer apertura de turno y manejar luego a traves del EventoTurnoCajaAperturado
                if (CentroNotificaciones.MostrarMensaje("No hay un turno de caja abierto para registrar este pago. ¿Desea abrir un turno ahora?", TipoMensajeEnum.Advertencia, BotonesMensajeEnum.SiNo) == DialogResult.Yes) {
                   AgregadorEventos.Publicar(new EventoMostrarVistaAperturaTurno { 
                       Venta = venta,
                       IdAlmacen = idAlmacen 
                   });
                }
                return;
            }

            // Crear el movimiento de caja
            var movimiento = new CajaMovimiento {
                IdTurno = turnoActivo.Id,
                Tipo = TipoMovimientoCajaEnum.Venta,
                CanalPago = e.Pago.CanalPago,
                IdVenta = e.Pago.IdVenta,
                Monto = e.Pago.MontoPagado,
                Descripcion = $"Pago venta - {venta.NumeroFacturaTicket}",
                IdCuentaUsuario = ContextoSeguridad.UsuarioAutenticado!.Id,
                FechaMovimiento = DateTime.Now
            };

            // Registrar el movimiento
            _repoCajaMovimiento.Adicionar(movimiento);

            // Publicar evento de movimiento registrado para notificar a otros componentes
            AgregadorEventos.Publicar(new EventoMovimientoCajaRegistrado { Movimiento = movimiento });
        }

        internal void Manejar(EventoPagoVentaAnulado e) {
            // Revertir el movimiento en caja cuando se anula un pago
            if (e.Pago == null)
                return;

            var venta = RepoVenta.Instancia.ObtenerPorId(e.Pago.IdVenta);
            var idAlmacen = venta!.IdAlmacenOrigen;
            var turnoActivo = _repoCajaTurno.ObtenerTurnoAbierto(idAlmacen);

            if (turnoActivo == null)
                return;

            // Crear movimiento de devolución
            var movimiento = new CajaMovimiento {
                IdTurno = turnoActivo.Id,
                Tipo = TipoMovimientoCajaEnum.DevolucionVenta,
                CanalPago = e.Pago.CanalPago,
                IdVenta = e.Pago.IdVenta,
                Monto = -Math.Abs(e.Pago.MontoPagado),
                Descripcion = $"Devolución/Anulación pago - {venta.NumeroFacturaTicket}",
                IdCuentaUsuario = ContextoSeguridad.UsuarioAutenticado?.Id ?? 0,
                FechaMovimiento = DateTime.Now
            };

            _repoCajaMovimiento.Adicionar(movimiento);

            AgregadorEventos.Publicar(new EventoMovimientoCajaRegistrado { Movimiento = movimiento });
        }
    }
}

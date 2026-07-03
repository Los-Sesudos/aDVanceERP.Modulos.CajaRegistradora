using aDVanceERP.Core.Eventos.Comun;
using aDVanceERP.Core.Eventos.Modulos.Caja;
using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Modelos.Modulos.Comun;
using aDVanceERP.Core.Repositorios.Modulos.Caja;
using aDVanceERP.Core.Repositorios.Modulos.Comun;

namespace aDVanceERP.Modulos.CajaRegistradora.Manejadores {
    internal class ManejadorAperturaTurno {
        private readonly RepoCajaTurno _repoCajaTurno = null!;
        private readonly RepoCajaMovimiento _repoCajaMovimiento = null!;
        private readonly RepoPago _repoPago = null!;

        internal ManejadorAperturaTurno() {
            _repoCajaTurno = RepoCajaTurno.Instancia;
            _repoCajaMovimiento = RepoCajaMovimiento.Instancia;
            _repoPago = RepoPago.Instancia;
        }

        internal void Manejar(EventoTurnoCajaAperturado e) {
            if (e.Venta == null)
                return;

            // Buscar el turno activo del almacén donde se realizó la venta
            var venta = e.Venta;
            var pago = _repoPago
                .Buscar(FiltroBusquedaPago.IdCompraVenta, e.Venta.Id.ToString(), "Venta")
                .resultadosBusqueda
                .FirstOrDefault()
                .entidadBase;

            if (pago == null)
                return;

            var idAlmacen = e.Turno.IdAlmacen;
            var turnoActivo = _repoCajaTurno.ObtenerTurnoAbierto(idAlmacen)!;

            // Crear el movimiento de caja
            var movimiento = new CajaMovimiento {
                IdTurno = turnoActivo.Id,
                Tipo = TipoMovimientoCajaEnum.Venta,
                CanalPago = pago.CanalPago,
                IdVenta = pago.IdVenta,
                Monto = pago.MontoPagado,
                Descripcion = $"Pago venta - {venta.NumeroFacturaTicket}",
                IdCuentaUsuario = ContextoSeguridad.UsuarioAutenticado!.Id,
                FechaMovimiento = DateTime.Now
            };

            // Registrar el movimiento
            _repoCajaMovimiento.Adicionar(movimiento);

            // Publicar evento de movimiento registrado para notificar a otros componentes
            AgregadorEventos.Publicar(new EventoMovimientoCajaRegistrado { Movimiento = movimiento });
        }
    }
}

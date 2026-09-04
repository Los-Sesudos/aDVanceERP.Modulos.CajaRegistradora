using aDVanceERP.Core.Eventos.Comun;
using aDVanceERP.Core.Eventos.Modulos.Caja;
using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Modelos.Comun;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Modelos.Modulos.Comun;
using aDVanceERP.Core.Repositorios.Modulos.Caja;
using aDVanceERP.Core.Repositorios.Modulos.Monedas;

namespace aDVanceERP.Modulos.CajaRegistradora.Manejadores {
    internal class ManejadorCierreTurno {
        private RepoCajaArqueo _repoCajaArqueo = null!;

        internal ManejadorCierreTurno() {
            _repoCajaArqueo = RepoCajaArqueo.Instancia;
        }

        internal void Manejar(EventoConfirmacionCierreTurno e) {
            var idUsuario = ContextoSeguridad.UsuarioAutenticado?.Id ?? 0;
            var idMonedaBase = RepoMoneda.Instancia.ObtenerMonedaBase().Id;

            _repoCajaArqueo.GuardarArqueoCompleto(e.Turno.Id, e.ArqueoCaja);

            var filaEfectivoBase = e.Conciliacion.FirstOrDefault(c => c.IdMoneda == idMonedaBase && c.CanalPago == CanalPagoEnum.Efectivo);
            var filaTransferenciaBase = e.Conciliacion.FirstOrDefault(c => c.IdMoneda == idMonedaBase && c.CanalPago == CanalPagoEnum.TransferenciaBancaria);

            // Ajustes por diferencia — uno por cada moneda/canal con descuadre, no solo el de la base.
            foreach (var fila in e.Conciliacion.Where(c => c.Diferencia != 0m)) {
                var moneda = RepoMoneda.Instancia.ObtenerPorId(fila.IdMoneda);
                RepoCajaMovimiento.Instancia.Adicionar(new CajaMovimiento {
                    IdTurno = e.Turno.Id,
                    Tipo = TipoMovimientoCajaEnum.AjusteArqueo,
                    CanalPago = fila.CanalPago,
                    IdMoneda = fila.IdMoneda,
                    IdVenta = null,
                    Monto = fila.Diferencia,
                    Descripcion = fila.Diferencia > 0
                        ? $"Sobrante de {fila.CanalPago} en arqueo de cierre ({moneda?.Codigo})"
                        : $"Faltante de {fila.CanalPago} en arqueo de cierre ({moneda?.Codigo})",
                    IdCuentaUsuario = idUsuario,
                    FechaMovimiento = DateTime.Now
                });
            }

            var cerrado = RepoCajaTurno.Instancia.CerrarTurno(
                idTurno: e.Turno.Id,
                idCuentaCierre: idUsuario,
                montoEfectivoCalculado: filaEfectivoBase?.MontoCalculado ?? 0,
                montoEfectivoDeclarado: filaEfectivoBase?.MontoDeclarado ?? 0,
                montoTransferenciasCalculado: filaTransferenciaBase?.MontoCalculado ?? 0,
                montoTransferenciasDeclarado: filaTransferenciaBase?.MontoDeclarado ?? 0,
                observacionesCierre: e.Observaciones);

            // El resto de monedas (no-base) se guarda aparte — CerrarTurno no tiene columnas para ellas.
            var conciliacionNoBase = e.Conciliacion.Where(c => c.IdMoneda != idMonedaBase).ToList();

            if (conciliacionNoBase.Count > 0)
                RepoCajaConciliacionMoneda.Instancia.GuardarConciliacion(e.Turno.Id, conciliacionNoBase);

            if (cerrado) {
                CentroNotificaciones.MostrarNotificacion($"Turno {e.Turno.Codigo} cerrado correctamente.", TipoNotificacionEnum.Info);
                AgregadorEventos.Publicar(new EventoTurnoCajaCerrado() { Turno = e.Turno });
            } else {
                CentroNotificaciones.MostrarNotificacion(
                    "No fue posible cerrar el turno. Es posible que ya haya sido cerrado en otra sesión.",
                    TipoNotificacionEnum.Error);
            }
        }
    }
}

using aDVanceERP.Core.Eventos.Comun;
using aDVanceERP.Core.Eventos.Modulos.Caja;
using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Modelos.Comun;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Modelos.Modulos.Comun;
using aDVanceERP.Core.Repositorios.Modulos.Caja;

namespace aDVanceERP.Modulos.CajaRegistradora.Manejadores {
    internal class ManejadorCierreTurno {
        private RepoCajaArqueo _repoCajaArqueo = null!;

        internal ManejadorCierreTurno() {
            _repoCajaArqueo = RepoCajaArqueo.Instancia;
        }

        internal void Manejar(EventoConfirmacionCierreTurno e) {
            var idUsuario = ContextoSeguridad.UsuarioAutenticado?.Id ?? 0;

            // Guardar arqueo de denominaciones (transacción interna en el repo)
            _repoCajaArqueo.GuardarArqueoCompleto(e.Turno!.Id, e.ArqueoCaja);

            // Registrar AjusteArqueo si hay diferencia en efectivo
            if (e.DiferenciaEfectivo != 0m) {
                RepoCajaMovimiento.Instancia.Adicionar(new CajaMovimiento {
                    IdTurno = e.Turno.Id,
                    Tipo = TipoMovimientoCajaEnum.AjusteArqueo,
                    CanalPago = CanalPagoEnum.Efectivo,
                    IdVenta = null,
                    Monto = e.DiferenciaEfectivo,
                    Descripcion = e.DiferenciaEfectivo > 0
                                        ? "Sobrante de efectivo en arqueo de cierre"
                                        : "Faltante de efectivo en arqueo de cierre",
                    IdCuentaUsuario = idUsuario,
                    FechaMovimiento = DateTime.Now
                });
            }

            // Cerrar el turno - el repo escribe los 4 montos y cambia el estado
            var cerrado = RepoCajaTurno.Instancia.CerrarTurno(
                idTurno: e.Turno.Id,
                idCuentaCierre: idUsuario,
                montoEfectivoCalculado: 0,//TODO: e.TotalesCierreCaja!.TotalEfectivo,
                montoEfectivoDeclarado: e.MontoEfectivoDeclarado,
                montoTransferenciasCalculado: 0, // TODO: e.TotalesCierreCaja.TotalTransferencias,
                montoTransferenciasDeclarado: e.MontoTransferenciasDeclarado,
                observacionesCierre: e.Observaciones);

            if (cerrado) {
                CentroNotificaciones.MostrarNotificacion(
                    $"Turno {e.Turno.Codigo} cerrado correctamente.",
                    TipoNotificacionEnum.Info);

                AgregadorEventos.Publicar(new EventoTurnoCajaCerrado() {
                    Turno = e.Turno
                });
            } else {
                CentroNotificaciones.MostrarNotificacion(
                    "No fue posible cerrar el turno. Es posible que ya haya sido cerrado en otra sesión.",
                    TipoNotificacionEnum.Error);
            }
        }
    }
}

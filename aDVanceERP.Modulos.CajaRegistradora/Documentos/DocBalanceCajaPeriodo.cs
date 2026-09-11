using aDVanceERP.Core.Documentos.Comun;
using aDVanceERP.Core.Infraestructura.Extensiones.BD;
using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Modelos.Modulos.Comun;
using aDVanceERP.Core.Repositorios.Modulos.Caja;
using aDVanceERP.Core.Repositorios.Modulos.Monedas;

using MySql.Data.MySqlClient;

using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

using System.Data;
using System.Diagnostics;
using System.Globalization;

namespace aDVanceERP.Modulos.CajaRegistradora.Documentos {

    /// <summary>
    /// Balance de caja acumulado para un rango de fechas: NO lista movimiento por movimiento
    /// (eso ya lo cubre DocResumenCaja / DocCierreCaja) — responde una pregunta distinta:
    /// ¿cuánto debería haber acumulado en caja en este período según lo que entró y salió,
    /// y eso coincide con lo que realmente se declaró en cada cierre de turno?
    ///
    /// Supuesto: el fondo de apertura (CajaTurno.MontoApertura) siempre está en moneda base
    /// — no tiene id_moneda propio — así que el "saldo inicial" del período solo aplica a la
    /// moneda base. Para el resto de monedas el período arranca en 0 y su saldo teórico es
    /// puramente el neto de movimientos.
    /// </summary>
    internal class DocBalanceCajaPeriodo : DocumentoBase {
        private readonly DateTime _fechaDesde;
        private readonly DateTime _fechaHasta;
        private string? _rutaLogoEmpresa;

        private List<Dictionary<string, string>> _turnosCerrados; // turnos cerrados con fecha_cierre en el rango
        private decimal _saldoInicialBase;
        private List<TotalMovimientoPorTipo> _movimientosPorTipo;
        private List<FilaBalanceMoneda> _balancePorMoneda;
        private List<Dictionary<string, string>> _turnosConDiferencia;

        #region Constructor

        public DocBalanceCajaPeriodo(DateTime fechaDesde, DateTime fechaHasta) {
            _fechaDesde = fechaDesde.Date;
            _fechaHasta = fechaHasta.Date;

            InicializarEmpresaYLogo();
        }

        private void InicializarEmpresaYLogo() {
            CargarInformacionEmpresa();

            string rutaLogo = !string.IsNullOrEmpty(_rutaLogoEmpresa) && File.Exists(_rutaLogoEmpresa)
                ? _rutaLogoEmpresa
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.png");

            if (File.Exists(rutaLogo)) CargarLogo(rutaLogo);
        }

        private string SubtituloBanner => $"{_fechaDesde:dd/MM/yyyy} — {_fechaHasta:dd/MM/yyyy}";
        private string TituloDocumento => "BALANCE DE CAJA DEL PERÍODO";
        private string TextoPiePagina => $"Balance de caja: {_fechaDesde:dd/MM/yyyy} — {_fechaHasta:dd/MM/yyyy}";

        #endregion

        #region Empresa

        protected override void CargarInformacionEmpresa() {
            try {
                using var connection = new MySqlConnection(ContextoBaseDatos.Configuracion.ToStringConexion());
                connection.Open();

                const string query = """
                    SELECT nombre, razon_social, rif, direccion, telefono, email, web, ruta_logo
                    FROM adv__empresa
                    LIMIT 1
                    """;

                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                if (reader.Read()) {
                    string nombre = (!reader.IsDBNull("razon_social") && !string.IsNullOrWhiteSpace(reader["razon_social"].ToString()))
                        ? reader["razon_social"].ToString()!
                        : reader["nombre"].ToString()!;

                    ConfigurarEmpresa(
                        nombre: nombre,
                        direccion: reader.IsDBNull("direccion") ? string.Empty : reader["direccion"].ToString()!,
                        telefono: reader.IsDBNull("telefono") ? string.Empty : reader["telefono"].ToString()!,
                        email: reader.IsDBNull("email") ? string.Empty : reader["email"].ToString()!,
                        web: reader.IsDBNull("web") ? string.Empty : reader["web"].ToString()!,
                        rif: reader.IsDBNull("rif") ? string.Empty : reader["rif"].ToString()!
                    );

                    if (!reader.IsDBNull("ruta_logo")) {
                        _rutaLogoEmpresa = reader["ruta_logo"].ToString();
                    }
                }
            } catch (Exception ex) {
                Debug.WriteLine($"[CargarInformacionEmpresa] Error: {ex.Message}");
            }
        }

        #endregion

        #region Generación de PDF

        public override void GenerarDocumento(bool mostrar = true) {
            try {
                _turnosCerrados = ObtenerTurnosCerradosEnRango();
                _saldoInicialBase = ObtenerFondoAperturaPrimerTurno();
                _movimientosPorTipo = RepoCajaMovimiento.Instancia.ObtenerTotalesPorTipoYMonedaEnRango(_fechaDesde, _fechaHasta);
                _balancePorMoneda = ConstruirBalancePorMoneda();
                _turnosConDiferencia = ObtenerTurnosConDiferencia();

                var idMonedaBase = RepoMoneda.Instancia.ObtenerMonedaBase().Id;
                var simboloBase = RepoMoneda.Instancia.ObtenerPorId(idMonedaBase)?.Simbolo ?? "$";

                var documento = new PdfDocument();
                documento.Info.Title = $"Balance de Caja {_fechaDesde:dd-MM-yyyy} a {_fechaHasta:dd-MM-yyyy}";
                documento.Info.Author = NombreEmpresa;
                documento.Info.Creator = "aDVance ERP";

                var pagina = documento.AddPage();
                pagina.Size = PageSize.Letter;
                var gfx = XGraphics.FromPdfPage(pagina);
                int paginaActual = 1;

                double yPos = DibujarEncabezadoDocumento(gfx, pagina, TituloDocumento, SubtituloBanner, DateTime.Now, primeraPagina: true);
                double anchoDisponible = gfx.PageSize.Width - MargenIzquierdo - MargenDerecho;
                double anchoCuadro = (anchoDisponible - 15) / 2;
                decimal totalTeoricoBase = _balancePorMoneda.Sum(f => f.SaldoTeoricoBase);
                decimal totalDeclaradoBase = _balancePorMoneda.Sum(f => f.SaldoDeclaradoBase);
                decimal diferenciaAcumuladaBase = totalDeclaradoBase - totalTeoricoBase;

                // ===== Cuadros: período | resumen general =====
                DibujarCuadroInformacion(gfx, MargenIzquierdo, yPos, anchoCuadro, "PERÍODO", new Dictionary<string, string> {
                    ["Desde"] = _fechaDesde.ToString("dd/MM/yyyy"),
                    ["Hasta"] = _fechaHasta.ToString("dd/MM/yyyy"),
                    ["Turnos cerrados"] = _turnosCerrados.Count.ToString(),
                    ["Turnos con diferencia"] = _turnosConDiferencia.Count.ToString()
                });

                DibujarCuadroInformacion(gfx, MargenIzquierdo + anchoCuadro + 15, yPos, anchoCuadro, "RESUMEN GENERAL (moneda base)", new Dictionary<string, string> {
                    ["Saldo teórico final"] = $"{simboloBase} {totalTeoricoBase:N2}",
                    ["Saldo declarado real"] = $"{simboloBase} {totalDeclaradoBase:N2}",
                    ["Diferencia acumulada"] = $"{simboloBase} {diferenciaAcumuladaBase:N2}",
                });

                yPos += 100;

                // ===== Tabla: movimientos netos por tipo y moneda =====
                gfx.DrawString("MOVIMIENTOS NETOS DEL PERÍODO, POR TIPO Y MONEDA", FontEncabezado, new XSolidBrush(ColorPrimario), new XPoint(MargenIzquierdo, yPos));
                yPos += 20;

                var columnasTipo = new Dictionary<string, double> {
                    ["Moneda"] = anchoDisponible * 0.20,
                    ["Tipo de movimiento"] = anchoDisponible * 0.50,
                    ["Neto"] = anchoDisponible * 0.30
                };

                DibujarEncabezadoTabla(gfx, yPos, columnasTipo);
                yPos += 25;

                int fila = 0;
                foreach (var m in _movimientosPorTipo.OrderBy(m => m.IdMoneda).ThenBy(m => m.Tipo)) {
                    if (NecesitaNuevaPagina(yPos, 20, gfx.PageSize.Height)) {
                        DibujarPiePagina(gfx, pagina, paginaActual, paginaActual + 1, TextoPiePagina);

                        pagina = documento.AddPage();
                        paginaActual++;
                        pagina.Size = PageSize.Letter;
                        gfx = XGraphics.FromPdfPage(pagina);

                        yPos = DibujarEncabezadoDocumento(gfx, pagina, TituloDocumento, SubtituloBanner, DateTime.Now, primeraPagina: false) + 10;
                        DibujarEncabezadoTabla(gfx, yPos, columnasTipo);
                        yPos += 25;
                    }

                    fila++;
                    var moneda = RepoMoneda.Instancia.ObtenerPorId(m.IdMoneda);
                    string simbolo = moneda?.Simbolo ?? "$";

                    var datos = new Dictionary<string, (double ancho, string valor, XStringFormat formato)> {
                        ["Moneda"] = (columnasTipo["Moneda"], moneda?.Codigo ?? "N/A", XStringFormats.CenterLeft),
                        ["Tipo de movimiento"] = (columnasTipo["Tipo de movimiento"], NombreTipoMovimiento(m.Tipo), XStringFormats.CenterLeft),
                        ["Neto"] = (columnasTipo["Neto"], $"{simbolo} {m.Total:N2}", XStringFormats.CenterRight)
                    };

                    DibujarFilaTabla(gfx, yPos, datos, fila % 2 == 0, 20);
                    yPos += 20;
                }

                yPos += 25;

                // ===== Tabla: balance por moneda (lo importante del documento) =====
                if (NecesitaNuevaPagina(yPos, 60, gfx.PageSize.Height)) {
                    DibujarPiePagina(gfx, pagina, paginaActual, paginaActual + 1, TextoPiePagina);

                    pagina = documento.AddPage();
                    paginaActual++;
                    pagina.Size = PageSize.Letter;
                    gfx = XGraphics.FromPdfPage(pagina);

                    yPos = DibujarEncabezadoDocumento(gfx, pagina, TituloDocumento, SubtituloBanner, DateTime.Now, primeraPagina: false) + 10;
                }

                gfx.DrawString("BALANCE POR MONEDA", FontEncabezado, new XSolidBrush(ColorPrimario), new XPoint(MargenIzquierdo, yPos));
                yPos += 20;

                var columnasBalance = new Dictionary<string, double> {
                    ["Moneda"] = anchoDisponible * 0.14,
                    ["Saldo inicial"] = anchoDisponible * 0.16,
                    ["+ Movimientos"] = anchoDisponible * 0.16,
                    ["= Teórico"] = anchoDisponible * 0.16,
                    ["Declarado real"] = anchoDisponible * 0.16,
                    ["Diferencia"] = anchoDisponible * 0.22
                };

                DibujarEncabezadoTabla(gfx, yPos, columnasBalance);
                yPos += 25;

                fila = 0;
                foreach (var b in _balancePorMoneda) {
                    if (NecesitaNuevaPagina(yPos, 20, gfx.PageSize.Height)) {
                        DibujarPiePagina(gfx, pagina, paginaActual, paginaActual + 1, TextoPiePagina);

                        pagina = documento.AddPage();
                        paginaActual++;
                        pagina.Size = PageSize.Letter;
                        gfx = XGraphics.FromPdfPage(pagina);

                        yPos = DibujarEncabezadoDocumento(gfx, pagina, TituloDocumento, SubtituloBanner, DateTime.Now, primeraPagina: false) + 10;
                        DibujarEncabezadoTabla(gfx, yPos, columnasBalance);
                        yPos += 25;
                    }

                    fila++;
                    var datos = new Dictionary<string, (double ancho, string valor, XStringFormat formato)> {
                        ["Moneda"] = (columnasBalance["Moneda"], b.CodigoMoneda, XStringFormats.CenterLeft),
                        ["Saldo inicial"] = (columnasBalance["Saldo inicial"], $"{b.Simbolo} {b.SaldoInicial:N2}", XStringFormats.CenterRight),
                        ["+ Movimientos"] = (columnasBalance["+ Movimientos"], $"{b.Simbolo} {b.NetoMovimientos:N2}", XStringFormats.CenterRight),
                        ["= Teórico"] = (columnasBalance["= Teórico"], $"{b.Simbolo} {b.SaldoTeorico:N2}", XStringFormats.CenterRight),
                        ["Declarado real"] = (columnasBalance["Declarado real"], $"{b.Simbolo} {b.SaldoDeclarado:N2}", XStringFormats.CenterRight),
                        ["Diferencia"] = (columnasBalance["Diferencia"], $"{b.Simbolo} {b.Diferencia:N2}", XStringFormats.CenterRight)
                    };

                    DibujarFilaTabla(gfx, yPos, datos, fila % 2 == 0, 20);
                    yPos += 20;
                }

                yPos += 10;
                gfx.DrawLine(new XPen(ColorSecundario, 2), MargenIzquierdo, yPos, gfx.PageSize.Width - MargenDerecho, yPos);
                yPos += 20;

                DibujarSeccionTotales(gfx, yPos, new Dictionary<string, string> {
                    ["TOTAL TEÓRICO"] = $"{simboloBase} {totalTeoricoBase:N2}",
                    ["TOTAL DECLARADO"] = $"{simboloBase} {totalDeclaradoBase:N2}",
                    ["DIFERENCIA ACUMULADA"] = $"{simboloBase} {diferenciaAcumuladaBase:N2}"
                }, 300);

                yPos += 90;

                // ===== Apéndice: turnos con diferencia, para ir directo a investigar =====
                if (_turnosConDiferencia.Count > 0) {
                    if (NecesitaNuevaPagina(yPos, 60, gfx.PageSize.Height)) {
                        DibujarPiePagina(gfx, pagina, paginaActual, paginaActual + 1, TextoPiePagina);

                        pagina = documento.AddPage();
                        paginaActual++;
                        pagina.Size = PageSize.Letter;
                        gfx = XGraphics.FromPdfPage(pagina);

                        yPos = DibujarEncabezadoDocumento(gfx, pagina, TituloDocumento, SubtituloBanner, DateTime.Now, primeraPagina: false) + 10;
                    }

                    gfx.DrawString("TURNOS CON DIFERENCIA (para investigar)", FontEncabezado, new XSolidBrush(ColorPrimario), new XPoint(MargenIzquierdo, yPos));
                    yPos += 20;

                    var columnasTd = new Dictionary<string, double> {
                        ["Código"] = anchoDisponible * 0.22,
                        ["Cierre"] = anchoDisponible * 0.18,
                        ["Moneda"] = anchoDisponible * 0.14,
                        ["Canal"] = anchoDisponible * 0.20,
                        ["Diferencia"] = anchoDisponible * 0.26
                    };

                    DibujarEncabezadoTabla(gfx, yPos, columnasTd);
                    yPos += 25;

                    fila = 0;
                    foreach (var t in _turnosConDiferencia) {
                        if (NecesitaNuevaPagina(yPos, 20, gfx.PageSize.Height)) {
                            DibujarPiePagina(gfx, pagina, paginaActual, paginaActual + 1, TextoPiePagina);

                            pagina = documento.AddPage();
                            paginaActual++;
                            pagina.Size = PageSize.Letter;
                            gfx = XGraphics.FromPdfPage(pagina);

                            yPos = DibujarEncabezadoDocumento(gfx, pagina, TituloDocumento, SubtituloBanner, DateTime.Now, primeraPagina: false) + 10;
                            DibujarEncabezadoTabla(gfx, yPos, columnasTd);
                            yPos += 25;
                        }

                        fila++;
                        var datos = new Dictionary<string, (double ancho, string valor, XStringFormat formato)> {
                            ["Código"] = (columnasTd["Código"], t["codigo"], XStringFormats.CenterLeft),
                            ["Cierre"] = (columnasTd["Cierre"], t["fecha_cierre"], XStringFormats.CenterLeft),
                            ["Moneda"] = (columnasTd["Moneda"], t["moneda"], XStringFormats.CenterLeft),
                            ["Canal"] = (columnasTd["Canal"], t["canal"], XStringFormats.CenterLeft),
                            ["Diferencia"] = (columnasTd["Diferencia"], $"{t["simbolo"]} {decimal.Parse(t["diferencia"], CultureInfo.InvariantCulture):N2}", XStringFormats.CenterRight)
                        };

                        DibujarFilaTabla(gfx, yPos, datos, fila % 2 == 0, 20);
                        yPos += 20;
                    }
                }

                DibujarPiePagina(gfx, pagina, paginaActual, paginaActual, TextoPiePagina);

                string rutaDocumento = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"BalanceCaja_{_fechaDesde:yyyyMMdd}_{_fechaHasta:yyyyMMdd}_{DateTime.Now:HHmmss}.pdf");

                documento.Save(rutaDocumento);

                if (mostrar) {
                    Process.Start(new ProcessStartInfo { FileName = rutaDocumento, UseShellExecute = true });
                }

                CentroNotificaciones.MostrarNotificacion(
                    "Balance de caja del período generado exitosamente.",
                    Core.Modelos.Comun.TipoNotificacionEnum.Info
                );

            } catch (Exception ex) {
                CentroNotificaciones.MostrarNotificacion(
                    $"Error al generar el balance de caja del período: {ex.Message}",
                    Core.Modelos.Comun.TipoNotificacionEnum.Error
                );
            }
        }

        #endregion

        #region Consultas

        private List<Dictionary<string, string>> ObtenerTurnosCerradosEnRango() {
            var turnos = new List<Dictionary<string, string>>();

            using var connection = new MySqlConnection(ContextoBaseDatos.Configuracion.ToStringConexion());
            connection.Open();

            const string query = """
                SELECT ct.id_turno, ct.codigo, ct.fecha_apertura, ct.fecha_cierre, ct.monto_apertura
                FROM adv__caja_turno ct
                WHERE ct.estado = 'Cerrado'
                  AND DATE(ct.fecha_cierre) BETWEEN @desde AND @hasta
                ORDER BY ct.fecha_apertura;
                """;

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@desde", _fechaDesde);
            command.Parameters.AddWithValue("@hasta", _fechaHasta);

            using var reader = command.ExecuteReader();
            while (reader.Read()) {
                turnos.Add(new Dictionary<string, string> {
                    ["id_turno"] = Convert.ToInt64(reader["id_turno"]).ToString(CultureInfo.InvariantCulture),
                    ["codigo"] = reader["codigo"]?.ToString() ?? "",
                    ["fecha_apertura"] = Convert.ToDateTime(reader["fecha_apertura"]).ToString("dd/MM/yyyy HH:mm"),
                    ["fecha_cierre"] = Convert.ToDateTime(reader["fecha_cierre"]).ToString("dd/MM/yyyy HH:mm"),
                    ["monto_apertura"] = Convert.ToDecimal(reader["monto_apertura"]).ToString(CultureInfo.InvariantCulture)
                });
            }

            return turnos;
        }

        /// <summary>
        /// Fondo de apertura del primer turno CUYA APERTURA cae dentro del rango
        /// (independientemente de si ya cerró o sigue abierto). Si no hay ningún turno
        /// abierto en el rango, devuelve 0 — no hay saldo inicial que atribuirle al período.
        /// </summary>
        private decimal ObtenerFondoAperturaPrimerTurno() {
            const string query = """
                SELECT monto_apertura
                FROM adv__caja_turno
                WHERE DATE(fecha_apertura) BETWEEN @desde AND @hasta
                ORDER BY fecha_apertura ASC
                LIMIT 1;
                """;

            var parametros = new Dictionary<string, object> {
                { "@desde", _fechaDesde },
                { "@hasta", _fechaHasta }
            };

            var resultado = ContextoBaseDatos.EjecutarConsultaEscalar<object>(query, parametros);
            return resultado != null && resultado != DBNull.Value ? Convert.ToDecimal(resultado) : 0m;
        }

        /// <summary>
        /// Une, por moneda: saldo inicial (solo aplica a la moneda base) + neto de movimientos
        /// = saldo teórico, contra lo realmente declarado en los cierres del rango
        /// (mismo criterio de fuente que DocResumenCaja.ConstruirFilasConciliacion:
        /// moneda base desde las columnas planas de adv__caja_turno, el resto desde
        /// adv__caja_conciliacion_moneda).
        /// </summary>
        private List<FilaBalanceMoneda> ConstruirBalancePorMoneda() {
            var idMonedaBase = RepoMoneda.Instancia.ObtenerMonedaBase().Id;

            var resultado = new Dictionary<long, FilaBalanceMoneda>();

            FilaBalanceMoneda ObtenerOCrear(long idMoneda) {
                if (!resultado.TryGetValue(idMoneda, out var f)) {
                    var moneda = RepoMoneda.Instancia.ObtenerPorId(idMoneda);
                    f = new FilaBalanceMoneda {
                        IdMoneda = idMoneda,
                        CodigoMoneda = moneda?.Codigo ?? "N/A",
                        Simbolo = moneda?.Simbolo ?? "$"
                    };
                    resultado[idMoneda] = f;
                }
                return f;
            }

            // Saldo inicial: solo la moneda base — ya está en base, sin conversión.
            var filaBase = ObtenerOCrear(idMonedaBase);
            filaBase.SaldoInicial = _saldoInicialBase;
            filaBase.SaldoInicialBase = _saldoInicialBase;

            // Neto de movimientos, por moneda — nativo Y ya congelado en base.
            foreach (var m in _movimientosPorTipo) {
                var fila = ObtenerOCrear(m.IdMoneda);
                fila.NetoMovimientos += m.Total;
                fila.NetoMovimientosBase += m.TotalBase;
            }

            // Declarado real: moneda base desde adv__caja_turno (columnas planas, ya en base),
            // resto desde adv__caja_conciliacion_moneda usando MontoDeclaradoBase (tasa
            // congelada al momento del cierre, no la de hoy).
            decimal efectivoDeclaradoBase = _turnosCerrados.Sum(t =>
                ContextoBaseDatos.EjecutarConsultaEscalar<decimal?>(
                    "SELECT COALESCE(monto_efectivo_declarado,0) + COALESCE(monto_transferencias_declarado,0) FROM adv__caja_turno WHERE id_turno = @id;",
                    new Dictionary<string, object> { { "@id", long.Parse(t["id_turno"]) } }) ?? 0m);

            filaBase.SaldoDeclarado += efectivoDeclaradoBase;
            filaBase.SaldoDeclaradoBase += efectivoDeclaradoBase;

            foreach (var t in _turnosCerrados) {
                var idTurno = long.Parse(t["id_turno"]);
                foreach (var c in RepoCajaConciliacionMoneda.Instancia.ObtenerPorTurno(idTurno).Where(c => c.IdMoneda != idMonedaBase)) {
                    var fila = ObtenerOCrear(c.IdMoneda);
                    fila.SaldoDeclarado += c.MontoDeclarado;
                    fila.SaldoDeclaradoBase += c.MontoDeclaradoBase;
                }
            }

            return resultado.Values.OrderByDescending(f => f.IdMoneda == idMonedaBase).ThenBy(f => f.CodigoMoneda).ToList();
        }

        /// <summary>
        /// Turnos cerrados en el rango cuya conciliación (cualquier moneda/canal) no cuadró.
        /// </summary>
        private List<Dictionary<string, string>> ObtenerTurnosConDiferencia() {
            var idMonedaBase = RepoMoneda.Instancia.ObtenerMonedaBase().Id;
            var lista = new List<Dictionary<string, string>>();

            foreach (var t in _turnosCerrados) {
                var idTurno = long.Parse(t["id_turno"]);

                // Moneda base: diferencia_efectivo + diferencia_transferencias (columnas planas).
                var difBase = ContextoBaseDatos.EjecutarConsultaEscalar<decimal?>(
                    "SELECT COALESCE(diferencia_efectivo,0) + COALESCE(diferencia_transferencias,0) FROM adv__caja_turno WHERE id_turno = @id;",
                    new Dictionary<string, object> { { "@id", idTurno } }) ?? 0m;

                if (difBase != 0m) {
                    lista.Add(new Dictionary<string, string> {
                        ["codigo"] = t["codigo"],
                        ["fecha_cierre"] = t["fecha_cierre"],
                        ["moneda"] = RepoMoneda.Instancia.ObtenerPorId(idMonedaBase)?.Codigo ?? "N/A",
                        ["canal"] = "Efectivo + Transferencias",
                        ["diferencia"] = difBase.ToString(CultureInfo.InvariantCulture),
                        ["simbolo"] = RepoMoneda.Instancia.ObtenerPorId(idMonedaBase)?.Simbolo ?? "$"
                    });
                }

                foreach (var c in RepoCajaConciliacionMoneda.Instancia.ObtenerPorTurno(idTurno).Where(c => c.IdMoneda != idMonedaBase && c.Diferencia != 0m)) {
                    var moneda = RepoMoneda.Instancia.ObtenerPorId(c.IdMoneda);
                    lista.Add(new Dictionary<string, string> {
                        ["codigo"] = t["codigo"],
                        ["fecha_cierre"] = t["fecha_cierre"],
                        ["moneda"] = moneda?.Codigo ?? "N/A",
                        ["canal"] = c.CanalPago.ToString(),
                        ["diferencia"] = c.Diferencia.ToString(CultureInfo.InvariantCulture),
                        ["simbolo"] = moneda?.Simbolo ?? "$"
                    });
                }
            }

            return lista;
        }

        private static string NombreTipoMovimiento(TipoMovimientoCajaEnum tipo) => tipo switch {
            TipoMovimientoCajaEnum.Venta => "Venta",
            TipoMovimientoCajaEnum.DevolucionVenta => "Devolución venta",
            TipoMovimientoCajaEnum.EntradaManual => "Entrada manual",
            TipoMovimientoCajaEnum.SalidaManual => "Salida manual",
            TipoMovimientoCajaEnum.AjusteArqueo => "Ajuste arqueo",
            _ => tipo.ToString()
        };

        #endregion
    }

    /// <summary>
    /// DTO interno: una fila del balance por moneda del período.
    /// </summary>
    internal sealed class FilaBalanceMoneda {
        public long IdMoneda { get; set; }
        public string CodigoMoneda { get; set; } = string.Empty;
        public string Simbolo { get; set; } = "$";

        public decimal SaldoInicial { get; set; }
        public decimal SaldoInicialBase { get; set; }

        public decimal NetoMovimientos { get; set; }
        public decimal NetoMovimientosBase { get; set; }

        public decimal SaldoTeorico => SaldoInicial + NetoMovimientos;
        public decimal SaldoTeoricoBase => SaldoInicialBase + NetoMovimientosBase;

        public decimal SaldoDeclarado { get; set; }
        public decimal SaldoDeclaradoBase { get; set; }

        public decimal Diferencia => SaldoDeclarado - SaldoTeorico;
        public decimal DiferenciaBase => SaldoDeclaradoBase - SaldoTeoricoBase;
    }

}
using aDVanceERP.Core.Documentos.Comun;
using aDVanceERP.Core.Infraestructura.Extensiones.BD;
using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Repositorios.Modulos.Caja;
using aDVanceERP.Core.Repositorios.Modulos.Monedas;

using MySql.Data.MySqlClient;

using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

using System.Diagnostics;
using System.Globalization;

namespace aDVanceERP.Modulos.CajaRegistradora.Documentos {
    /// <summary>
    /// Cierre de caja del día: todos los movimientos de adv__caja_movimiento del día,
    /// agrupados por turno, con el cuadre (apertura / calculado / declarado / diferencia)
    /// de cada turno. Multimoneda: cada movimiento y cada diferencia de conciliación
    /// se maneja en su propia moneda; el total general del pie se muestra convertido a
    /// moneda base — sumar monedas distintas tal cual es un error, no un total.
    /// </summary>
    internal class DocCierreCaja : DocumentoBase {
        private readonly DateTime _fecha;
        private List<Dictionary<string, string>> _movimientos;
        private List<Dictionary<string, string>> _turnos;
        private List<Dictionary<string, string>> _diferenciasPorMoneda; // una fila por (turno, moneda, canal) con diferencia != 0

        #region Constructor

        public DocCierreCaja(DateTime fecha) {
            _fecha = fecha.Date;

            CargarInformacionEmpresa();

            string rutaLogo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.png");
            if (File.Exists(rutaLogo)) CargarLogo(rutaLogo);
        }

        #endregion

        public override void GenerarDocumento(bool mostrar = true) {
            try {
                _turnos = ObtenerTurnosDelDia(_fecha);
                _movimientos = ObtenerMovimientosDelDia(_fecha);

                if (_turnos.Count == 0) {
                    CentroNotificaciones.MostrarNotificacion(
                        $"No hay turnos de caja registrados el {_fecha:dd/MM/yyyy}.",
                        Core.Modelos.Comun.TipoNotificacionEnum.Advertencia);
                    return;
                }

                var idMonedaBase = RepoMoneda.Instancia.ObtenerMonedaBase().Id;
                var simboloBase = RepoMoneda.Instancia.ObtenerPorId(idMonedaBase)?.Simbolo ?? "$";

                _diferenciasPorMoneda = ObtenerDiferenciasPorMoneda(idMonedaBase);

                var documento = new PdfDocument();
                documento.Info.Title = $"Cierre de Caja {_fecha:dd-MM-yyyy}";
                documento.Info.Author = NombreEmpresa;
                documento.Info.Creator = "aDVance ERP";

                var pagina = documento.AddPage();
                pagina.Size = PageSize.Letter;
                var gfx = XGraphics.FromPdfPage(pagina);
                int paginaActual = 1;

                DibujarBannerProfesional(gfx, pagina, "CIERRE DE CAJA", _fecha.ToString("dd/MM/yyyy"), _fecha);

                double yPos = ObtenerInicioPosicionContenido();
                double anchoDisponible = gfx.PageSize.Width - MargenIzquierdo - MargenDerecho;

                // ===== Totales generales del día, agrupados por moneda y consolidados en base =====
                var totalesPorMoneda = _movimientos
                    .GroupBy(m => m["id_moneda"])
                    .Select(g => {
                        var idMoneda = long.Parse(g.Key);
                        decimal entradas = g.Where(m => decimal.Parse(m["monto"], CultureInfo.InvariantCulture) > 0)
                                            .Sum(m => decimal.Parse(m["monto"], CultureInfo.InvariantCulture));
                        decimal salidas = g.Where(m => decimal.Parse(m["monto"], CultureInfo.InvariantCulture) < 0)
                                           .Sum(m => decimal.Parse(m["monto"], CultureInfo.InvariantCulture));
                        // Base ya congelada por movimiento (monto_moneda_base) — no se recalcula aquí.
                        decimal entradasBase = g.Where(m => decimal.Parse(m["monto"], CultureInfo.InvariantCulture) > 0)
                                                .Sum(m => decimal.Parse(m["monto_base"], CultureInfo.InvariantCulture));
                        decimal salidasBase = g.Where(m => decimal.Parse(m["monto"], CultureInfo.InvariantCulture) < 0)
                                               .Sum(m => decimal.Parse(m["monto_base"], CultureInfo.InvariantCulture));
                        return (idMoneda, codigo: g.First()["codigo_moneda"], simbolo: g.First()["simbolo_moneda"], entradas, salidas, entradasBase, salidasBase);
                    })
                    .ToList();

                decimal totalEntradasBase = totalesPorMoneda.Sum(t => t.entradasBase);
                decimal totalSalidasBase = totalesPorMoneda.Sum(t => t.salidasBase);
                decimal netoDiaBase = totalEntradasBase + totalSalidasBase;
                decimal diferenciaTotalDiaBase = _diferenciasPorMoneda.Sum(d => decimal.Parse(d["diferencia_base"], CultureInfo.InvariantCulture));


                double anchoCuadro = (anchoDisponible - 15) / 2;

                DibujarCuadroInformacion(gfx, MargenIzquierdo, yPos, anchoCuadro, "MOVIMIENTOS DEL DÍA (moneda base)", new Dictionary<string, string> {
                    ["Turnos del día"] = _turnos.Count.ToString(),
                    ["Total entradas"] = $"{simboloBase} {totalEntradasBase:N2}",
                    ["Total salidas"] = $"{simboloBase} {Math.Abs(totalSalidasBase):N2}",
                    ["Neto del día"] = $"{simboloBase} {netoDiaBase:N2}",
                });

                DibujarCuadroInformacion(gfx, MargenIzquierdo + anchoCuadro + 15, yPos, anchoCuadro, "CUADRE (moneda base)", new Dictionary<string, string> {
                    ["Diferencia total del día"] = $"{simboloBase} {diferenciaTotalDiaBase:N2}",
                    ["Estado"] = Math.Abs(diferenciaTotalDiaBase) < 0.01m ? "Cuadrado" : (diferenciaTotalDiaBase < 0 ? "Faltante" : "Sobrante"),
                });

                yPos += 110;

                // ===== Movimientos y cuadre por moneda (si hay más de una) =====
                if (totalesPorMoneda.Count > 1 || totalesPorMoneda.Any(t => t.idMoneda != idMonedaBase)) {
                    gfx.DrawString("DESGLOSE POR MONEDA", FontEncabezado, new XSolidBrush(ColorPrimario), new XPoint(MargenIzquierdo, yPos));
                    yPos += 20;

                    var columnasMoneda = new Dictionary<string, double> {
                        ["Moneda"] = anchoDisponible * 0.18,
                        ["Entradas"] = anchoDisponible * 0.24,
                        ["Salidas"] = anchoDisponible * 0.24,
                        ["Neto"] = anchoDisponible * 0.34
                    };

                    DibujarEncabezadoTabla(gfx, yPos, columnasMoneda);
                    yPos += 25;

                    int filaM = 0;
                    foreach (var t in totalesPorMoneda.OrderByDescending(t => t.idMoneda == idMonedaBase)) {
                        filaM++;
                        var datos = new Dictionary<string, (double ancho, string valor, XStringFormat formato)> {
                            ["Moneda"] = (columnasMoneda["Moneda"], t.codigo, XStringFormats.CenterLeft),
                            ["Entradas"] = (columnasMoneda["Entradas"], $"{t.simbolo} {t.entradas:N2}", XStringFormats.CenterRight),
                            ["Salidas"] = (columnasMoneda["Salidas"], $"{t.simbolo} {Math.Abs(t.salidas):N2}", XStringFormats.CenterRight),
                            ["Neto"] = (columnasMoneda["Neto"], $"{t.simbolo} {(t.entradas + t.salidas):N2}", XStringFormats.CenterRight)
                        };

                        DibujarFilaTabla(gfx, yPos, datos, filaM % 2 == 0, 20);
                        yPos += 20;
                    }

                    yPos += 25;
                }

                // ===== Tabla de turnos =====
                if (NecesitaNuevaPagina(yPos, 25 + (_turnos.Count * 20), gfx.PageSize.Height)) {
                    gfx.Dispose();

                    pagina = documento.AddPage();
                    pagina.Size = PageSize.Letter;
                    paginaActual++;
                    gfx = XGraphics.FromPdfPage(pagina);

                    DibujarBannerProfesional(gfx, pagina, "CIERRE DE CAJA", _fecha.ToString("dd/MM/yyyy"), _fecha);
                    yPos = ObtenerInicioPosicionContenido() + 10;
                }

                gfx.DrawString("TURNOS DEL DÍA", FontEncabezado, new XSolidBrush(ColorPrimario), new XPoint(MargenIzquierdo, yPos));
                yPos += 20;

                var columnasTurno = new Dictionary<string, double> {
                    ["Código"] = anchoDisponible * 0.18,
                    ["Almacén"] = anchoDisponible * 0.18,
                    ["Apertura"] = anchoDisponible * 0.11,
                    ["Calculado"] = anchoDisponible * 0.13,
                    ["Declarado"] = anchoDisponible * 0.13,
                    ["Diferencia"] = anchoDisponible * 0.11,
                    ["Estado"] = anchoDisponible * 0.16
                };

                DibujarEncabezadoTabla(gfx, yPos, columnasTurno);
                yPos += 25;

                int fila = 0;
                foreach (var turno in _turnos) {
                    fila++;
                    // Calculado/Declarado/Diferencia de esta tabla siguen siendo SOLO moneda base
                    // (son las columnas planas de adv__caja_turno, por diseño). La diferencia en
                    // otras monedas está en la sección "DESGLOSE POR MONEDA" y en el cuadro CUADRE.
                    decimal diferenciaTurnoBase = decimal.Parse(turno["diferencia_efectivo"], CultureInfo.InvariantCulture)
                                             + decimal.Parse(turno["diferencia_transferencias"], CultureInfo.InvariantCulture);

                    var datos = new Dictionary<string, (double ancho, string valor, XStringFormat formato)> {
                        ["Código"] = (columnasTurno["Código"], turno["codigo"], XStringFormats.CenterLeft),
                        ["Almacén"] = (columnasTurno["Almacén"], TruncarTexto(turno["almacen"], FontContenido, gfx, columnasTurno["Almacén"] - 10), XStringFormats.CenterLeft),
                        ["Apertura"] = (columnasTurno["Apertura"], $"{simboloBase} " + decimal.Parse(turno["monto_apertura"], CultureInfo.InvariantCulture).ToString("N2"), XStringFormats.CenterRight),
                        ["Calculado"] = (columnasTurno["Calculado"], $"{simboloBase} " + decimal.Parse(turno["monto_efectivo_calculado"], CultureInfo.InvariantCulture).ToString("N2"), XStringFormats.CenterRight),
                        ["Declarado"] = (columnasTurno["Declarado"], $"{simboloBase} " + decimal.Parse(turno["monto_efectivo_declarado"], CultureInfo.InvariantCulture).ToString("N2"), XStringFormats.CenterRight),
                        ["Diferencia"] = (columnasTurno["Diferencia"], $"{simboloBase} " + diferenciaTurnoBase.ToString("N2"), XStringFormats.CenterRight),
                        ["Estado"] = (columnasTurno["Estado"], turno["estado"], XStringFormats.CenterLeft)
                    };

                    DibujarFilaTabla(gfx, yPos, datos, fila % 2 == 0, 20);
                    yPos += 20;
                }

                // ===== Diferencias en monedas no-base (si las hay) =====
                if (_diferenciasPorMoneda.Any(d => long.Parse(d["id_moneda"]) != idMonedaBase)) {
                    yPos += 25;

                    if (NecesitaNuevaPagina(yPos, 45, gfx.PageSize.Height)) {
                        gfx.Dispose();

                        pagina = documento.AddPage();
                        pagina.Size = PageSize.Letter;
                        paginaActual++;
                        gfx = XGraphics.FromPdfPage(pagina);

                        DibujarBannerProfesional(gfx, pagina, "CIERRE DE CAJA", _fecha.ToString("dd/MM/yyyy"), _fecha);
                        yPos = ObtenerInicioPosicionContenido() + 10;
                    }

                    gfx.DrawString("DIFERENCIAS EN OTRAS MONEDAS", FontEncabezado, new XSolidBrush(ColorPrimario), new XPoint(MargenIzquierdo, yPos));
                    yPos += 20;

                    var columnasDif = new Dictionary<string, double> {
                        ["Turno"] = anchoDisponible * 0.20,
                        ["Moneda"] = anchoDisponible * 0.16,
                        ["Canal"] = anchoDisponible * 0.32,
                        ["Diferencia"] = anchoDisponible * 0.32
                    };

                    DibujarEncabezadoTabla(gfx, yPos, columnasDif);
                    yPos += 25;

                    int filaD = 0;
                    foreach (var d in _diferenciasPorMoneda.Where(d => long.Parse(d["id_moneda"]) != idMonedaBase)) {
                        if (NecesitaNuevaPagina(yPos, 20, gfx.PageSize.Height)) {
                            gfx.Dispose();

                            pagina = documento.AddPage();
                            pagina.Size = PageSize.Letter;
                            paginaActual++;
                            gfx = XGraphics.FromPdfPage(pagina);

                            DibujarBannerProfesional(gfx, pagina, "CIERRE DE CAJA", _fecha.ToString("dd/MM/yyyy"), _fecha);
                            yPos = ObtenerInicioPosicionContenido() + 10;
                            DibujarEncabezadoTabla(gfx, yPos, columnasDif);
                            yPos += 25;
                        }

                        filaD++;
                        var datos = new Dictionary<string, (double ancho, string valor, XStringFormat formato)> {
                            ["Turno"] = (columnasDif["Turno"], d["codigo_turno"], XStringFormats.CenterLeft),
                            ["Moneda"] = (columnasDif["Moneda"], d["codigo_moneda"], XStringFormats.CenterLeft),
                            ["Canal"] = (columnasDif["Canal"], d["canal"], XStringFormats.CenterLeft),
                            ["Diferencia"] = (columnasDif["Diferencia"], $"{d["simbolo_moneda"]} " + decimal.Parse(d["diferencia"], CultureInfo.InvariantCulture).ToString("N2"), XStringFormats.CenterRight)
                        };

                        DibujarFilaTabla(gfx, yPos, datos, filaD % 2 == 0, 20);
                        yPos += 20;
                    }
                }

                yPos += 25;

                // ===== Tabla de movimientos =====
                gfx.DrawString("MOVIMIENTOS DE CAJA", FontEncabezado, new XSolidBrush(ColorPrimario), new XPoint(MargenIzquierdo, yPos));
                yPos += 20;

                var columnasMov = new Dictionary<string, double> {
                    ["Hora"] = anchoDisponible * 0.08,
                    ["Turno"] = anchoDisponible * 0.09,
                    ["Tipo"] = anchoDisponible * 0.11,
                    ["Moneda"] = anchoDisponible * 0.08,
                    ["Canal"] = anchoDisponible * 0.11,
                    ["Monto"] = anchoDisponible * 0.13,
                    ["Usuario"] = anchoDisponible * 0.13,
                    ["Descripción"] = anchoDisponible * 0.27
                };

                DibujarEncabezadoTabla(gfx, yPos, columnasMov);
                yPos += 25;

                fila = 0;
                foreach (var mov in _movimientos) {
                    if (NecesitaNuevaPagina(yPos, 20, gfx.PageSize.Height)) {
                        gfx.Dispose();

                        pagina = documento.AddPage();
                        pagina.Size = PageSize.Letter;
                        paginaActual++;
                        gfx = XGraphics.FromPdfPage(pagina);

                        DibujarBannerProfesional(
                            gfx,
                            pagina,
                            "CIERRE DE CAJA",
                            _fecha.ToString("dd/MM/yyyy"),
                            _fecha);

                        yPos = ObtenerInicioPosicionContenido() + 10;

                        gfx.DrawString(
                            "MOVIMIENTOS DE CAJA",
                            FontEncabezado,
                            new XSolidBrush(ColorPrimario),
                            new XPoint(MargenIzquierdo, yPos));

                        yPos += 20;
                        DibujarEncabezadoTabla(gfx, yPos, columnasMov);
                        yPos += 25;
                    }

                    fila++;
                    decimal monto = decimal.Parse(mov["monto"], CultureInfo.InvariantCulture);
                    string simboloMov = mov["simbolo_moneda"];

                    var datos = new Dictionary<string, (double ancho, string valor, XStringFormat formato)> {
                        ["Hora"] = (columnasMov["Hora"], mov["hora"], XStringFormats.CenterLeft),
                        ["Turno"] = (columnasMov["Turno"], mov["codigo_turno"].Split("-")[2], XStringFormats.CenterLeft),
                        ["Tipo"] = (columnasMov["Tipo"], mov["tipo"], XStringFormats.CenterLeft),
                        ["Moneda"] = (columnasMov["Moneda"], mov["codigo_moneda"], XStringFormats.CenterLeft),
                        ["Canal"] = (columnasMov["Canal"], mov["canal_pago"], XStringFormats.CenterLeft),
                        ["Monto"] = (columnasMov["Monto"], $"{simboloMov} " + monto.ToString("N2"), XStringFormats.CenterRight),
                        ["Usuario"] = (columnasMov["Usuario"], TruncarTexto(mov["usuario"], FontContenido, gfx, columnasMov["Usuario"] - 10), XStringFormats.CenterLeft),
                        ["Descripción"] = (columnasMov["Descripción"], TruncarTexto(mov["descripcion"], FontContenido, gfx, columnasMov["Descripción"] - 10), XStringFormats.CenterLeft)
                    };

                    DibujarFilaTabla(gfx, yPos, datos, fila % 2 == 0, 20);
                    yPos += 20;
                }

                // Reservar el bloque final completo; nunca dejar que los totales
                // queden cortados al final de una página.
                const double alturaTotales = 110;

                if (NecesitaNuevaPagina(
                    yPos,
                    alturaTotales,
                    gfx.PageSize.Height)) {

                    gfx.Dispose();

                    pagina = documento.AddPage();
                    pagina.Size = PageSize.Letter;
                    paginaActual++;
                    gfx = XGraphics.FromPdfPage(pagina);

                    DibujarBannerProfesional(
                        gfx,
                        pagina,
                        "CIERRE DE CAJA",
                        _fecha.ToString("dd/MM/yyyy"),
                        _fecha);

                    yPos = ObtenerInicioPosicionContenido() + 10;
                }

                yPos += 15;

                gfx.DrawLine(
                    new XPen(ColorSecundario, 2),
                    MargenIzquierdo,
                    yPos,
                    gfx.PageSize.Width - MargenDerecho,
                    yPos);

                yPos += 15;

                DibujarSeccionTotales(
                    gfx,
                    yPos,
                    new Dictionary<string, string> {
                        ["TOTAL ENTRADAS (base)"] = $"{simboloBase} {totalEntradasBase:N2}",
                        ["TOTAL SALIDAS (base)"] = $"{simboloBase} {Math.Abs(totalSalidasBase):N2}",
                        ["NETO DEL DÍA (base)"] = $"{simboloBase} {netoDiaBase:N2}"
                    },
                    300);

                // El pie definitivo se dibuja en un único pase final.
                gfx.Dispose();
                ActualizarNumeracionPaginas(
                    documento,
                    _fecha);

                string rutaDocumento = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"CierreCaja_{_fecha:yyyyMMdd}_{DateTime.Now:HHmmss}.pdf"
                );

                documento.Save(rutaDocumento);

                if (mostrar) {
                    Process.Start(new ProcessStartInfo { FileName = rutaDocumento, UseShellExecute = true });
                }

                CentroNotificaciones.MostrarNotificacion(
                    "Cierre de caja generado exitosamente.",
                    Core.Modelos.Comun.TipoNotificacionEnum.Info
                );

            } catch (Exception ex) {
                CentroNotificaciones.MostrarNotificacion(
                    $"Error al generar el cierre de caja: {ex.Message}",
                    Core.Modelos.Comun.TipoNotificacionEnum.Error
                );
            }
        }

        private void ActualizarNumeracionPaginas(
            PdfDocument documento,
            DateTime fecha) {

            int totalPaginas = documento.Pages.Count;

            for (int i = 0; i < totalPaginas; i++) {
                var pagina = documento.Pages[i];

                using var gfxPie = XGraphics.FromPdfPage(
                    pagina,
                    XGraphicsPdfPageOptions.Append);

                DibujarPiePagina(
                    gfxPie,
                    pagina,
                    i + 1,
                    totalPaginas,
                    $"Cierre de caja: {fecha:dd/MM/yyyy}");
            }
        }

        #region Consultas

        private static List<Dictionary<string, string>> ObtenerTurnosDelDia(DateTime fecha) {
            var turnos = new List<Dictionary<string, string>>();

            using (var connection = new MySqlConnection(ContextoBaseDatos.Configuracion.ToStringConexion())) {
                connection.Open();

                var query = """
                    SELECT
                        ct.codigo,
                        a.nombre AS almacen,
                        ct.monto_apertura,
                        COALESCE(ct.monto_efectivo_calculado, 0) AS monto_efectivo_calculado,
                        COALESCE(ct.monto_efectivo_declarado, 0) AS monto_efectivo_declarado,
                        COALESCE(ct.diferencia_efectivo, 0) AS diferencia_efectivo,
                        COALESCE(ct.diferencia_transferencias, 0) AS diferencia_transferencias,
                        ct.estado
                    FROM adv__caja_turno ct
                    INNER JOIN adv__almacen a ON ct.id_almacen = a.id_almacen
                    WHERE DATE(ct.fecha_apertura) = @fecha
                       OR DATE(ct.fecha_cierre) = @fecha
                    ORDER BY ct.fecha_apertura
                    """;

                using (var command = new MySqlCommand(query, connection)) {
                    command.Parameters.AddWithValue("@fecha", fecha);

                    using (var reader = command.ExecuteReader()) {
                        while (reader.Read()) {
                            turnos.Add(new Dictionary<string, string> {
                                ["codigo"] = reader["codigo"]?.ToString() ?? "",
                                ["almacen"] = reader["almacen"]?.ToString() ?? "",
                                ["monto_apertura"] = Convert.ToDecimal(reader["monto_apertura"]).ToString("N2", CultureInfo.InvariantCulture),
                                ["monto_efectivo_calculado"] = Convert.ToDecimal(reader["monto_efectivo_calculado"]).ToString("N2", CultureInfo.InvariantCulture),
                                ["monto_efectivo_declarado"] = Convert.ToDecimal(reader["monto_efectivo_declarado"]).ToString("N2", CultureInfo.InvariantCulture),
                                ["diferencia_efectivo"] = Convert.ToDecimal(reader["diferencia_efectivo"]).ToString("N2", CultureInfo.InvariantCulture),
                                ["diferencia_transferencias"] = Convert.ToDecimal(reader["diferencia_transferencias"]).ToString("N2", CultureInfo.InvariantCulture),
                                ["estado"] = reader["estado"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            return turnos;
        }

        private static List<Dictionary<string, string>> ObtenerMovimientosDelDia(DateTime fecha) {
            var movimientos = new List<Dictionary<string, string>>();

            using (var connection = new MySqlConnection(ContextoBaseDatos.Configuracion.ToStringConexion())) {
                connection.Open();

                var query = """
                    SELECT
                        cm.fecha_movimiento,
                        ct.codigo AS codigo_turno,
                        cm.tipo,
                        cm.canal_pago,
                        cm.id_moneda,
                        m.codigo  AS codigo_moneda,
                        m.simbolo AS simbolo_moneda,
                        cm.monto,
                        cm.monto_moneda_base,
                        cu.nombre AS usuario,
                        COALESCE(cm.descripcion, '') AS descripcion
                    FROM adv__caja_movimiento cm
                    INNER JOIN adv__caja_turno ct    ON cm.id_turno  = ct.id_turno
                    INNER JOIN adv__cuenta_usuario cu ON cm.id_cuenta_usuario = cu.id_cuenta_usuario
                    INNER JOIN adv__moneda m         ON cm.id_moneda = m.id_moneda
                    WHERE DATE(cm.fecha_movimiento) = @fecha
                    ORDER BY cm.fecha_movimiento
                    """;

                using (var command = new MySqlCommand(query, connection)) {
                    command.Parameters.AddWithValue("@fecha", fecha);

                    using (var reader = command.ExecuteReader()) {
                        while (reader.Read()) {
                            var fechaMov = Convert.ToDateTime(reader["fecha_movimiento"]);
                            movimientos.Add(new Dictionary<string, string> {
                                ["hora"] = fechaMov.ToString("HH:mm"),
                                ["codigo_turno"] = reader["codigo_turno"]?.ToString() ?? "",
                                ["tipo"] = reader["tipo"]?.ToString() ?? "",
                                ["canal_pago"] = reader["canal_pago"]?.ToString() ?? "",
                                ["id_moneda"] = Convert.ToInt64(reader["id_moneda"]).ToString(CultureInfo.InvariantCulture),
                                ["codigo_moneda"] = reader["codigo_moneda"]?.ToString() ?? "",
                                ["simbolo_moneda"] = reader["simbolo_moneda"]?.ToString() ?? "$",
                                ["monto"] = Convert.ToDecimal(reader["monto"]).ToString("N2", CultureInfo.InvariantCulture),
                                ["monto_base"] = Convert.ToDecimal(reader["monto_moneda_base"]).ToString("N2", CultureInfo.InvariantCulture),
                                ["usuario"] = reader["usuario"]?.ToString() ?? "",
                                ["descripcion"] = reader["descripcion"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            return movimientos;
        }

        /// <summary>
        /// Diferencias de conciliación del día, una fila por (turno, moneda, canal) con
        /// diferencia != 0. Moneda base: diferencia_efectivo + diferencia_transferencias
        /// de adv__caja_turno (solo turnos cerrados ese día). Resto de monedas: desde
        /// adv__caja_conciliacion_moneda, filtrando IdMoneda != base para no duplicar
        /// si esa tabla también llegara a guardar filas de la moneda base — PENDIENTE
        /// confirmar con el flujo de guardado de cierre si eso puede pasar.
        /// </summary>
        private List<Dictionary<string, string>> ObtenerDiferenciasPorMoneda(long idMonedaBase) {
            var resultado = new List<Dictionary<string, string>>();
            var simboloBase = RepoMoneda.Instancia.ObtenerPorId(idMonedaBase)?.Simbolo ?? "$";
            var codigoBase = RepoMoneda.Instancia.ObtenerPorId(idMonedaBase)?.Codigo ?? "N/A";

            using var connection = new MySqlConnection(ContextoBaseDatos.Configuracion.ToStringConexion());
            connection.Open();

            const string queryTurnosCerrados = """
                SELECT ct.id_turno, ct.codigo,
                       COALESCE(ct.diferencia_efectivo, 0) + COALESCE(ct.diferencia_transferencias, 0) AS diferencia_base
                FROM adv__caja_turno ct
                WHERE DATE(ct.fecha_cierre) = @fecha
                  AND ct.estado = 'Cerrado';
                """;

            using (var command = new MySqlCommand(queryTurnosCerrados, connection)) {
                command.Parameters.AddWithValue("@fecha", _fecha);
                using var reader = command.ExecuteReader();
                while (reader.Read()) {
                    var idTurno = Convert.ToInt64(reader["id_turno"]);
                    var codigoTurno = reader["codigo"]?.ToString() ?? "";
                    var diferenciaBase = Convert.ToDecimal(reader["diferencia_base"]);

                    if (diferenciaBase != 0m) {
                        resultado.Add(new Dictionary<string, string> {
                            ["codigo_turno"] = codigoTurno,
                            ["id_moneda"] = idMonedaBase.ToString(CultureInfo.InvariantCulture),
                            ["codigo_moneda"] = codigoBase,
                            ["simbolo_moneda"] = simboloBase,
                            ["canal"] = "Efectivo + Transferencias",
                            ["diferencia"] = diferenciaBase.ToString(CultureInfo.InvariantCulture),
                            ["diferencia_base"] = diferenciaBase.ToString(CultureInfo.InvariantCulture)
                        });
                    }

                    foreach (var c in RepoCajaConciliacionMoneda.Instancia.ObtenerPorTurno(idTurno).Where(c => c.IdMoneda != idMonedaBase && c.Diferencia != 0m)) {
                        var moneda = RepoMoneda.Instancia.ObtenerPorId(c.IdMoneda);
                        resultado.Add(new Dictionary<string, string> {
                            ["codigo_turno"] = codigoTurno,
                            ["id_moneda"] = c.IdMoneda.ToString(CultureInfo.InvariantCulture),
                            ["codigo_moneda"] = moneda?.Codigo ?? "N/A",
                            ["simbolo_moneda"] = moneda?.Simbolo ?? "$",
                            ["canal"] = c.CanalPago.ToString(),
                            ["diferencia"] = c.Diferencia.ToString(CultureInfo.InvariantCulture),
                            ["diferencia_base"] = c.DiferenciaBase.ToString(CultureInfo.InvariantCulture)
                        });
                    }
                }
            }

            return resultado;
        }

        #endregion

        #region Auxiliares

        private string TruncarTexto(string texto, XFont fuente, XGraphics gfx, double anchoMaximo) {
            var tamano = gfx.MeasureString(texto, fuente);
            if (tamano.Width <= anchoMaximo) return texto;

            var textoTruncado = texto;
            while (textoTruncado.Length > 0) {
                textoTruncado = textoTruncado.Substring(0, textoTruncado.Length - 1);
                if (gfx.MeasureString(textoTruncado + "...", fuente).Width <= anchoMaximo) {
                    return textoTruncado + "...";
                }
            }
            return "...";
        }

        #endregion
    }
}
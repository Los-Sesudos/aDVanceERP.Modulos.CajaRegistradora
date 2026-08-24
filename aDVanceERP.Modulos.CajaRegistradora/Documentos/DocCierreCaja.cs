using aDVanceERP.Core.Documentos.Comun;
using aDVanceERP.Core.Infraestructura.Extensiones.BD;
using aDVanceERP.Core.Infraestructura.Globales;

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
    /// de cada turno tomado directamente de adv__caja_turno.
    /// </summary>
    internal class DocCierreCaja : DocumentoBase {
        private readonly DateTime _fecha;
        private List<Dictionary<string, string>> _movimientos;
        private List<Dictionary<string, string>> _turnos;

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

                // ===== Totales generales del día =====
                decimal totalEntradas = _movimientos.Where(m => decimal.Parse(m["monto"], CultureInfo.InvariantCulture) > 0)
                                                     .Sum(m => decimal.Parse(m["monto"], CultureInfo.InvariantCulture));
                decimal totalSalidas = _movimientos.Where(m => decimal.Parse(m["monto"], CultureInfo.InvariantCulture) < 0)
                                                    .Sum(m => decimal.Parse(m["monto"], CultureInfo.InvariantCulture));
                decimal netoDia = totalEntradas + totalSalidas;
                decimal diferenciaTotalDia = _turnos.Sum(t => decimal.Parse(t["diferencia_efectivo"], CultureInfo.InvariantCulture)
                                                             + decimal.Parse(t["diferencia_transferencias"], CultureInfo.InvariantCulture));

                double anchoCuadro = (anchoDisponible - 15) / 2;

                DibujarCuadroInformacion(gfx, MargenIzquierdo, yPos, anchoCuadro, "MOVIMIENTOS DEL DÍA", new Dictionary<string, string> {
                    ["Turnos del día"] = _turnos.Count.ToString(),
                    ["Total entradas"] = "$" + totalEntradas.ToString("N2"),
                    ["Total salidas"] = "$" + Math.Abs(totalSalidas).ToString("N2"),
                    ["Neto del día"] = "$" + netoDia.ToString("N2"),
                });

                DibujarCuadroInformacion(gfx, MargenIzquierdo + anchoCuadro + 15, yPos, anchoCuadro, "CUADRE", new Dictionary<string, string> {
                    ["Diferencia total del día"] = "$" + diferenciaTotalDia.ToString("N2"),
                    ["Estado"] = Math.Abs(diferenciaTotalDia) < 0.01m ? "Cuadrado" : (diferenciaTotalDia < 0 ? "Faltante" : "Sobrante"),
                });

                yPos += 110;

                // ===== Tabla de turnos =====
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
                    decimal diferenciaTurno = decimal.Parse(turno["diferencia_efectivo"], CultureInfo.InvariantCulture)
                                             + decimal.Parse(turno["diferencia_transferencias"], CultureInfo.InvariantCulture);

                    var datos = new Dictionary<string, (double ancho, string valor, XStringFormat formato)> {
                        ["Código"] = (columnasTurno["Código"], turno["codigo"], XStringFormats.CenterLeft),
                        ["Almacén"] = (columnasTurno["Almacén"], TruncarTexto(turno["almacen"], FontContenido, gfx, columnasTurno["Almacén"] - 10), XStringFormats.CenterLeft),
                        ["Apertura"] = (columnasTurno["Apertura"], "$" + decimal.Parse(turno["monto_apertura"], CultureInfo.InvariantCulture).ToString("N2"), XStringFormats.CenterRight),
                        ["Calculado"] = (columnasTurno["Calculado"], "$" + decimal.Parse(turno["monto_efectivo_calculado"], CultureInfo.InvariantCulture).ToString("N2"), XStringFormats.CenterRight),
                        ["Declarado"] = (columnasTurno["Declarado"], "$" + decimal.Parse(turno["monto_efectivo_declarado"], CultureInfo.InvariantCulture).ToString("N2"), XStringFormats.CenterRight),
                        ["Diferencia"] = (columnasTurno["Diferencia"], "$" + diferenciaTurno.ToString("N2"), XStringFormats.CenterRight),
                        ["Estado"] = (columnasTurno["Estado"], turno["estado"], XStringFormats.CenterLeft)
                    };

                    DibujarFilaTabla(gfx, yPos, datos, fila % 2 == 0, 20);
                    yPos += 20;
                }

                yPos += 25;

                // ===== Tabla de movimientos =====
                gfx.DrawString("MOVIMIENTOS DE CAJA", FontEncabezado, new XSolidBrush(ColorPrimario), new XPoint(MargenIzquierdo, yPos));
                yPos += 20;

                var columnasMov = new Dictionary<string, double> {
                    ["Hora"] = anchoDisponible * 0.08,
                    ["Turno"] = anchoDisponible * 0.10,
                    ["Tipo"] = anchoDisponible * 0.12,
                    ["Canal"] = anchoDisponible * 0.13,
                    ["Monto"] = anchoDisponible * 0.13,
                    ["Usuario"] = anchoDisponible * 0.14,
                    ["Descripción"] = anchoDisponible * 0.30
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

                    var datos = new Dictionary<string, (double ancho, string valor, XStringFormat formato)> {
                        ["Hora"] = (columnasMov["Hora"], mov["hora"], XStringFormats.CenterLeft),
                        ["Turno"] = (columnasMov["Turno"], mov["codigo_turno"].Split("-")[2], XStringFormats.CenterLeft),
                        ["Tipo"] = (columnasMov["Tipo"], mov["tipo"], XStringFormats.CenterLeft),
                        ["Canal"] = (columnasMov["Canal"], mov["canal_pago"], XStringFormats.CenterLeft),
                        ["Monto"] = (columnasMov["Monto"], "$" + monto.ToString("N2"), XStringFormats.CenterRight),
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
                        ["TOTAL ENTRADAS"] = "$" + totalEntradas.ToString("N2"),
                        ["TOTAL SALIDAS"] = "$" + Math.Abs(totalSalidas).ToString("N2"),
                        ["NETO DEL DÍA"] = "$" + netoDia.ToString("N2")
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
                        cm.monto,
                        cu.nombre AS usuario,
                        COALESCE(cm.descripcion, '') AS descripcion
                    FROM adv__caja_movimiento cm
                    INNER JOIN adv__caja_turno ct ON cm.id_turno = ct.id_turno
                    INNER JOIN adv__cuenta_usuario cu ON cm.id_cuenta_usuario = cu.id_cuenta_usuario
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
                                ["monto"] = Convert.ToDecimal(reader["monto"]).ToString("N2", CultureInfo.InvariantCulture),
                                ["usuario"] = reader["usuario"]?.ToString() ?? "",
                                ["descripcion"] = reader["descripcion"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            return movimientos;
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
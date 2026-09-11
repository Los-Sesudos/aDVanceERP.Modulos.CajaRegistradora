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
    /// Resumen de caja — por un turno específico, o por todos los turnos cerrados de un día.
    /// Misma filosofía que DocResumenVentasDia: cuadros de resumen + tabla de detalle + totales,
    /// con la particularidad de que aquí el detalle es por moneda y canal, no por producto.
    /// Todo total general se muestra convertido a moneda base — sumar monedas distintas
    /// tal cual no es un total, es un error disfrazado de dato.
    /// </summary>
    internal class DocResumenCaja : DocumentoBase {
        private readonly DateTime? _fecha;
        private readonly long? _idTurno;
        private readonly bool _incluirDetalleMovimientos;
        private string? _rutaLogoEmpresa;

        private List<Dictionary<string, string>> _turnos;
        private List<Dictionary<string, string>> _filasConciliacion; // una fila por (moneda, canal)
        private List<CajaMovimiento> _movimientos;

        #region Constructores

        /// <summary>Resumen de un solo turno.</summary>
        /// <param name="incluirDetalleMovimientos">
        /// Si es true (default), agrega al final una sección con cada movimiento de caja
        /// del turno, agrupado por moneda. Pásalo en false para una versión más compacta.
        /// </param>
        public DocResumenCaja(long idTurno, bool incluirDetalleMovimientos = true) {
            _idTurno = idTurno;
            _fecha = null;
            _incluirDetalleMovimientos = incluirDetalleMovimientos;

            InicializarEmpresaYLogo();
        }

        /// <summary>Resumen de todos los turnos cerrados en un día.</summary>
        /// <param name="incluirDetalleMovimientos">
        /// Si es true (default), agrega al final una sección con cada movimiento de caja
        /// del día, agrupado por moneda. Pásalo en false para una versión más compacta.
        /// </param>
        public DocResumenCaja(DateTime fecha, bool incluirDetalleMovimientos = true) {
            _fecha = fecha.Date;
            _idTurno = null;
            _incluirDetalleMovimientos = incluirDetalleMovimientos;

            InicializarEmpresaYLogo();
        }

        private void InicializarEmpresaYLogo() {
            CargarInformacionEmpresa();

            string rutaLogo = !string.IsNullOrEmpty(_rutaLogoEmpresa) && File.Exists(_rutaLogoEmpresa)
                ? _rutaLogoEmpresa
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.png");

            if (File.Exists(rutaLogo)) CargarLogo(rutaLogo);
        }

        private bool ModoTurno => _idTurno.HasValue;
        private string TituloDocumento => ModoTurno ? "RESUMEN DE CAJA" : "RESUMEN DE CAJA DEL DÍA";
        private string SubtituloBanner => ModoTurno ? $"Turno #{_idTurno}" : _fecha!.Value.ToString("dd/MM/yyyy");
        private string TextoPiePagina => ModoTurno ? $"Resumen de caja: turno #{_idTurno}" : $"Resumen de caja: {_fecha!.Value:dd/MM/yyyy}";

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
                _turnos = ObtenerTurnos();

                if (_turnos.Count == 0) {
                    string mensajeVacio = ModoTurno
                        ? $"El turno #{_idTurno} no existe o no está cerrado."
                        : $"No hay turnos cerrados el {_fecha!.Value:dd/MM/yyyy}.";
                    CentroNotificaciones.MostrarNotificacion(mensajeVacio, Core.Modelos.Comun.TipoNotificacionEnum.Advertencia);
                    return;
                }

                _filasConciliacion = ConstruirFilasConciliacion();
                _movimientos = _incluirDetalleMovimientos ? ObtenerMovimientosDelPeriodo() : [];

                var documento = new PdfDocument();
                documento.Info.Title = ModoTurno ? $"Resumen de Caja — Turno #{_idTurno}" : $"Resumen de Caja {_fecha:dd-MM-yyyy}";
                documento.Info.Author = NombreEmpresa;
                documento.Info.Creator = "aDVance ERP";

                var pagina = documento.AddPage();
                pagina.Size = PageSize.Letter;
                var gfx = XGraphics.FromPdfPage(pagina);
                int paginaActual = 1;

                double yPos = DibujarEncabezadoDocumento(gfx, pagina, TituloDocumento, SubtituloBanner, DateTime.Now, primeraPagina: true);
                double anchoDisponible = gfx.PageSize.Width - MargenIzquierdo - MargenDerecho;
                double anchoCuadro = (anchoDisponible - 15) / 2;

                var idMonedaBase = RepoMoneda.Instancia.ObtenerMonedaBase().Id;
                var simboloBase = RepoMoneda.Instancia.ObtenerPorId(idMonedaBase)?.Simbolo ?? "$";

                decimal totalCalculadoBase = _filasConciliacion.Sum(f => decimal.Parse(f["calculado_base"], CultureInfo.InvariantCulture));
                decimal totalDeclaradoBase = _filasConciliacion.Sum(f => decimal.Parse(f["declarado_base"], CultureInfo.InvariantCulture));
                decimal diferenciaTotalBase = totalDeclaradoBase - totalCalculadoBase;


                // ===== Cuadros: turno(s) | resumen general en moneda base =====
                var camposTurno = new Dictionary<string, string>();
                if (ModoTurno) {
                    var t = _turnos[0];
                    camposTurno["Código"] = t["codigo"];
                    camposTurno["Almacén"] = t["almacen"];
                    camposTurno["Apertura"] = t["fecha_apertura"];
                    camposTurno["Cierre"] = t["fecha_cierre"];
                } else {
                    camposTurno["Turnos cerrados"] = _turnos.Count.ToString();
                    camposTurno["Almacenes"] = string.Join(", ", _turnos.Select(t => t["almacen"]).Distinct());
                }

                DibujarCuadroInformacion(gfx, MargenIzquierdo, yPos, anchoCuadro, ModoTurno ? "TURNO" : "TURNOS DEL DÍA", camposTurno);

                DibujarCuadroInformacion(gfx, MargenIzquierdo + anchoCuadro + 15, yPos, anchoCuadro, "RESUMEN GENERAL (moneda base)", new Dictionary<string, string> {
                    ["Total estimado"] = $"{simboloBase} {totalCalculadoBase:N2}",
                    ["Total declarado"] = $"{simboloBase} {totalDeclaradoBase:N2}",
                    ["Diferencia"] = $"{simboloBase} {diferenciaTotalBase:N2}",
                });

                yPos += 100;

                // ===== Tabla de conciliación por moneda y canal =====
                var columnas = new Dictionary<string, double> {
                    ["Moneda"] = anchoDisponible * 0.18,
                    ["Canal"] = anchoDisponible * 0.24,
                    ["Calculado"] = anchoDisponible * 0.19,
                    ["Declarado"] = anchoDisponible * 0.19,
                    ["Diferencia"] = anchoDisponible * 0.20
                };

                DibujarEncabezadoTabla(gfx, yPos, columnas);
                yPos += 25;

                int fila = 0;
                foreach (var f in _filasConciliacion) {
                    if (NecesitaNuevaPagina(yPos, 20, gfx.PageSize.Height)) {
                        DibujarPiePagina(gfx, pagina, paginaActual, paginaActual + 1, TextoPiePagina);

                        pagina = documento.AddPage();
                        paginaActual++;
                        pagina.Size = PageSize.Letter;
                        gfx = XGraphics.FromPdfPage(pagina);

                        yPos = DibujarEncabezadoDocumento(gfx, pagina, TituloDocumento, SubtituloBanner, DateTime.Now, primeraPagina: false) + 10;
                        DibujarEncabezadoTabla(gfx, yPos, columnas);
                        yPos += 25;
                    }

                    fila++;
                    decimal calculado = decimal.Parse(f["calculado"], CultureInfo.InvariantCulture);
                    decimal declarado = decimal.Parse(f["declarado"], CultureInfo.InvariantCulture);
                    decimal diferencia = declarado - calculado;
                    string simbolo = f["simbolo"];

                    var datos = new Dictionary<string, (double ancho, string valor, XStringFormat formato)> {
                        ["Moneda"] = (columnas["Moneda"], f["codigo_moneda"], XStringFormats.CenterLeft),
                        ["Canal"] = (columnas["Canal"], f["canal"], XStringFormats.CenterLeft),
                        ["Calculado"] = (columnas["Calculado"], $"{simbolo} {calculado:N2}", XStringFormats.CenterRight),
                        ["Declarado"] = (columnas["Declarado"], $"{simbolo} {declarado:N2}", XStringFormats.CenterRight),
                        ["Diferencia"] = (columnas["Diferencia"], $"{simbolo} {diferencia:N2}", XStringFormats.CenterRight)
                    };

                    DibujarFilaTabla(gfx, yPos, datos, fila % 2 == 0, 20);
                    yPos += 20;
                }

                // ===== Detalle de movimientos, agrupado por moneda =====
                if (_incluirDetalleMovimientos && _movimientos.Count > 0) {
                    yPos += 20;

                    if (NecesitaNuevaPagina(yPos, 40, gfx.PageSize.Height)) {
                        DibujarPiePagina(gfx, pagina, paginaActual, paginaActual + 1, TextoPiePagina);

                        pagina = documento.AddPage();
                        paginaActual++;
                        pagina.Size = PageSize.Letter;
                        gfx = XGraphics.FromPdfPage(pagina);

                        yPos = DibujarEncabezadoDocumento(gfx, pagina, TituloDocumento, SubtituloBanner, DateTime.Now, primeraPagina: false) + 10;
                    }

                    gfx.DrawString("DETALLE DE MOVIMIENTOS", FontEncabezado, new XSolidBrush(ColorPrimario), new XPoint(MargenIzquierdo, yPos));
                    yPos += 20;

                    var columnasMov = new Dictionary<string, double> {
                        ["Hora"] = anchoDisponible * 0.10,
                        ["Tipo"] = anchoDisponible * 0.20,
                        ["Canal"] = anchoDisponible * 0.16,
                        ["Monto"] = anchoDisponible * 0.16,
                        ["Usuario"] = anchoDisponible * 0.16,
                        ["Descripción"] = anchoDisponible * 0.22
                    };

                    // Un grupo por moneda — nunca se mezclan montos de distinta moneda en la misma tabla.
                    foreach (var grupoMoneda in _movimientos.GroupBy(m => m.IdMoneda)) {
                        var moneda = RepoMoneda.Instancia.ObtenerPorId(grupoMoneda.Key);
                        string simboloGrupo = moneda?.Simbolo ?? "$";

                        if (NecesitaNuevaPagina(yPos, 45, gfx.PageSize.Height)) {
                            DibujarPiePagina(gfx, pagina, paginaActual, paginaActual + 1, TextoPiePagina);

                            pagina = documento.AddPage();
                            paginaActual++;
                            pagina.Size = PageSize.Letter;
                            gfx = XGraphics.FromPdfPage(pagina);

                            yPos = DibujarEncabezadoDocumento(gfx, pagina, TituloDocumento, SubtituloBanner, DateTime.Now, primeraPagina: false) + 10;
                        }

                        gfx.DrawString($"— {moneda?.Codigo ?? "N/A"} —", FontContenido, new XSolidBrush(ColorTextoSecundario), new XPoint(MargenIzquierdo, yPos));
                        yPos += 16;

                        DibujarEncabezadoTabla(gfx, yPos, columnasMov);
                        yPos += 25;

                        int filaMov = 0;
                        foreach (var mov in grupoMoneda.OrderBy(m => m.FechaMovimiento)) {
                            if (NecesitaNuevaPagina(yPos, 20, gfx.PageSize.Height)) {
                                DibujarPiePagina(gfx, pagina, paginaActual, paginaActual + 1, TextoPiePagina);

                                pagina = documento.AddPage();
                                paginaActual++;
                                pagina.Size = PageSize.Letter;
                                gfx = XGraphics.FromPdfPage(pagina);

                                yPos = DibujarEncabezadoDocumento(gfx, pagina, TituloDocumento, SubtituloBanner, DateTime.Now, primeraPagina: false) + 10;

                                gfx.DrawString($"— {moneda?.Codigo ?? "N/A"} — (continuación)", FontContenido, new XSolidBrush(ColorTextoSecundario), new XPoint(MargenIzquierdo, yPos));
                                yPos += 16;

                                DibujarEncabezadoTabla(gfx, yPos, columnasMov);
                                yPos += 25;
                            }

                            filaMov++;

                            var datosMov = new Dictionary<string, (double ancho, string valor, XStringFormat formato)> {
                                ["Hora"] = (columnasMov["Hora"], mov.FechaMovimiento.ToString("dd/MM HH:mm"), XStringFormats.CenterLeft),
                                ["Tipo"] = (columnasMov["Tipo"], mov.Tipo.ToString(), XStringFormats.CenterLeft),
                                ["Canal"] = (columnasMov["Canal"], mov.CanalPago.ToString(), XStringFormats.CenterLeft),
                                ["Monto"] = (columnasMov["Monto"], $"{simboloGrupo} {mov.Monto:N2}", XStringFormats.CenterRight),
                                ["Usuario"] = (columnasMov["Usuario"], TruncarTexto(mov.NombreUsuario ?? "", FontContenido, gfx, columnasMov["Usuario"] - 10), XStringFormats.CenterLeft),
                                ["Descripción"] = (columnasMov["Descripción"], TruncarTexto(mov.Descripcion ?? "", FontContenido, gfx, columnasMov["Descripción"] - 10), XStringFormats.CenterLeft)
                            };

                            DibujarFilaTabla(gfx, yPos, datosMov, filaMov % 2 == 0, 20);
                            yPos += 20;
                        }

                        yPos += 20;
                    }
                }

                // ===== Totales (en moneda base) =====
                yPos += 10;
                gfx.DrawLine(new XPen(ColorSecundario, 2), MargenIzquierdo, yPos, gfx.PageSize.Width - MargenDerecho, yPos);
                yPos += 20;

                DibujarSeccionTotales(gfx, yPos, new Dictionary<string, string> {
                    ["TOTAL ESTIMADO"] = $"{simboloBase} {totalCalculadoBase:N2}",
                    ["TOTAL DECLARADO"] = $"{simboloBase} {totalDeclaradoBase:N2}",
                    ["DIFERENCIA TOTAL"] = $"{simboloBase} {diferenciaTotalBase:N2}"
                }, 300);

                yPos += 90;

                gfx.DrawString("* Los totales generales se calculan convirtiendo cada moneda a la moneda base con la tasa de cambio vigente. ",
                    FontPequeno, new XSolidBrush(ColorTextoSecundario),
                    new XPoint(MargenIzquierdo, yPos + 15));
                gfx.DrawString("  Los montos por fila se muestran en su moneda original, sin convertir.",
                    FontPequeno, new XSolidBrush(ColorTextoSecundario),
                    new XPoint(MargenIzquierdo, yPos + 25));

                DibujarPiePagina(gfx, pagina, paginaActual, paginaActual, TextoPiePagina);

                string nombreArchivo = ModoTurno
                    ? $"ResumenCaja_Turno{_idTurno}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                    : $"ResumenCaja_{_fecha:yyyyMMdd}_{DateTime.Now:HHmmss}.pdf";

                string rutaDocumento = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    nombreArchivo);

                documento.Save(rutaDocumento);

                if (mostrar) {
                    Process.Start(new ProcessStartInfo { FileName = rutaDocumento, UseShellExecute = true });
                }

                CentroNotificaciones.MostrarNotificacion(
                    "Resumen de caja generado exitosamente.",
                    Core.Modelos.Comun.TipoNotificacionEnum.Info
                );

            } catch (Exception ex) {
                CentroNotificaciones.MostrarNotificacion(
                    $"Error al generar el resumen de caja: {ex.Message}",
                    Core.Modelos.Comun.TipoNotificacionEnum.Error
                );
            }
        }

        #endregion

        #region Consultas

        private List<Dictionary<string, string>> ObtenerTurnos() {
            var turnos = new List<Dictionary<string, string>>();

            using var connection = new MySqlConnection(ContextoBaseDatos.Configuracion.ToStringConexion());
            connection.Open();

            string filtro = ModoTurno
                ? "ct.id_turno = @idTurno"
                : "DATE(ct.fecha_cierre) = @fecha AND ct.estado = 'Cerrado'";

            var query = $"""
                SELECT ct.id_turno, ct.codigo, a.nombre AS almacen,
                       ct.fecha_apertura, ct.fecha_cierre,
                       ct.monto_apertura,
                       ct.monto_efectivo_calculado, ct.monto_efectivo_declarado,
                       ct.monto_transferencias_calculado, ct.monto_transferencias_declarado
                FROM adv__caja_turno ct
                INNER JOIN adv__almacen a ON ct.id_almacen = a.id_almacen
                WHERE {filtro}
                ORDER BY ct.fecha_apertura
                """;

            using var command = new MySqlCommand(query, connection);
            if (ModoTurno) command.Parameters.AddWithValue("@idTurno", _idTurno!.Value);
            else command.Parameters.AddWithValue("@fecha", _fecha!.Value);

            using var reader = command.ExecuteReader();
            while (reader.Read()) {
                turnos.Add(new Dictionary<string, string> {
                    ["id_turno"] = Convert.ToInt64(reader["id_turno"]).ToString(CultureInfo.InvariantCulture),
                    ["codigo"] = reader["codigo"]?.ToString() ?? "",
                    ["almacen"] = reader["almacen"]?.ToString() ?? "",
                    ["fecha_apertura"] = Convert.ToDateTime(reader["fecha_apertura"]).ToString("dd/MM HH:mm"),
                    ["fecha_cierre"] = reader["fecha_cierre"] != DBNull.Value ? Convert.ToDateTime(reader["fecha_cierre"]).ToString("dd/MM HH:mm") : "—",
                    ["monto_efectivo_calculado"] = ConvertirDecimalSeguro(reader["monto_efectivo_calculado"]),
                    ["monto_efectivo_declarado"] = ConvertirDecimalSeguro(reader["monto_efectivo_declarado"]),
                    ["monto_transferencias_calculado"] = ConvertirDecimalSeguro(reader["monto_transferencias_calculado"]),
                    ["monto_transferencias_declarado"] = ConvertirDecimalSeguro(reader["monto_transferencias_declarado"])
                });
            }

            return turnos;
        }

        /// <summary>
        /// Junta, en filas (moneda, canal), la moneda base (que viene de las columnas planas
        /// de adv__caja_turno, sumadas si hay varios turnos) con el resto de monedas
        /// (que vienen de adv__caja_conciliacion_moneda, una consulta por turno incluido).
        /// </summary>
        private List<Dictionary<string, string>> ConstruirFilasConciliacion() {
            var idMonedaBase = RepoMoneda.Instancia.ObtenerMonedaBase().Id;
            var monedaBase = RepoMoneda.Instancia.ObtenerPorId(idMonedaBase);

            decimal efectivoCalculadoBase = _turnos.Sum(t => decimal.Parse(t["monto_efectivo_calculado"], CultureInfo.InvariantCulture));
            decimal efectivoDeclaradoBase = _turnos.Sum(t => decimal.Parse(t["monto_efectivo_declarado"], CultureInfo.InvariantCulture));
            decimal transferenciaCalculadaBase = _turnos.Sum(t => decimal.Parse(t["monto_transferencias_calculado"], CultureInfo.InvariantCulture));
            decimal transferenciaDeclaradaBase = _turnos.Sum(t => decimal.Parse(t["monto_transferencias_declarado"], CultureInfo.InvariantCulture));

            // Moneda base: calculado_base/declarado_base son iguales a calculado/declarado (tasa 1).
            var filas = new List<Dictionary<string, string>> {
                FilaConciliacion(idMonedaBase, monedaBase?.Codigo ?? "N/A", monedaBase?.Simbolo ?? "$", "Efectivo",
                    efectivoCalculadoBase, efectivoDeclaradoBase, efectivoCalculadoBase, efectivoDeclaradoBase),
                FilaConciliacion(idMonedaBase, monedaBase?.Codigo ?? "N/A", monedaBase?.Simbolo ?? "$", "Transferencia",
                    transferenciaCalculadaBase, transferenciaDeclaradaBase, transferenciaCalculadaBase, transferenciaDeclaradaBase)
            };

            // No-base: se acumula NATIVO y BASE por separado — el base se suma fila por fila
            // usando la tasa ya congelada de CADA registro (MontoCalculadoBase/MontoDeclaradoBase),
            // así que un turno cerrado con USD a 380 y otro con USD a 390 se convierten cada uno
            // con SU propia tasa antes de sumarse, en vez de aplicar una sola tasa (la de hoy) al final.
            var conciliacionNoBase = new Dictionary<(long idMoneda, CanalPagoEnum canal),
                (decimal calculado, decimal declarado, decimal calculadoBase, decimal declaradoBase)>();

            foreach (var t in _turnos) {
                var idTurno = long.Parse(t["id_turno"]);
                foreach (var c in RepoCajaConciliacionMoneda.Instancia.ObtenerPorTurno(idTurno).Where(c => c.IdMoneda != idMonedaBase)) {
                    var clave = (c.IdMoneda, c.CanalPago);
                    if (!conciliacionNoBase.TryGetValue(clave, out var acumulado)) acumulado = (0, 0, 0, 0);
                    conciliacionNoBase[clave] = (
                        acumulado.calculado + c.MontoCalculado,
                        acumulado.declarado + c.MontoDeclarado,
                        acumulado.calculadoBase + c.MontoCalculadoBase,
                        acumulado.declaradoBase + c.MontoDeclaradoBase
                    );
                }
            }

            foreach (var ((idMoneda, canal), (calculado, declarado, calculadoBase, declaradoBase)) in conciliacionNoBase) {
                var moneda = RepoMoneda.Instancia.ObtenerPorId(idMoneda);
                filas.Add(FilaConciliacion(idMoneda, moneda?.Codigo ?? "N/A", moneda?.Simbolo ?? "$",
                    canal == CanalPagoEnum.Efectivo ? "Efectivo" : "Transferencia",
                    calculado, declarado, calculadoBase, declaradoBase));
            }

            return filas;
        }


        private static Dictionary<string, string> FilaConciliacion(
        long idMoneda, string codigoMoneda, string simbolo, string canal,
        decimal calculado, decimal declarado, decimal calculadoBase, decimal declaradoBase) => new() {
            ["id_moneda"] = idMoneda.ToString(CultureInfo.InvariantCulture),
            ["codigo_moneda"] = codigoMoneda,
            ["simbolo"] = simbolo,
            ["canal"] = canal,
            ["calculado"] = calculado.ToString(CultureInfo.InvariantCulture),
            ["declarado"] = declarado.ToString(CultureInfo.InvariantCulture),
            ["calculado_base"] = calculadoBase.ToString(CultureInfo.InvariantCulture),
            ["declarado_base"] = declaradoBase.ToString(CultureInfo.InvariantCulture)
        };

        private static string ConvertirDecimalSeguro(object valor) =>
            (valor != DBNull.Value ? Convert.ToDecimal(valor) : 0m).ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Movimientos a listar en la sección de detalle: los del turno si ModoTurno,
        /// o los del día completo si es el resumen diario.
        /// </summary>
        private List<CajaMovimiento> ObtenerMovimientosDelPeriodo() =>
            ModoTurno
                ? RepoCajaMovimiento.Instancia.ObtenerMovimientosTurno(_idTurno!.Value)
                : RepoCajaMovimiento.Instancia.ObtenerMovimientosEnRango(_fecha!.Value, _fecha!.Value);

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
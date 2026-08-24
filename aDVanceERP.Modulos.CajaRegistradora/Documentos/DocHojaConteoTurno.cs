using aDVanceERP.Core.Documentos.Comun;
using aDVanceERP.Core.Infraestructura.Extensiones.BD;
using aDVanceERP.Core.Infraestructura.Globales;

using MySql.Data.MySqlClient;

using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

using System.Data;
using System.Diagnostics;
using System.Globalization;

namespace aDVanceERP.Modulos.CajaRegistradora.Documentos {
    /// <summary>
    /// Hoja base para apertura y cierre de turno por conteo.
    /// El inventario es dinámico y ocupa tantas páginas como sean necesarias.
    /// Los datos de producto, precio y existencia esperada se obtienen del sistema.
    /// El folio del turno se recibe por parámetro porque ya está disponible desde
    /// el registro que origina la generación del documento.
    /// </summary>
    internal class DocHojaConteoTurno : DocumentoBase {
        private readonly long _idAlmacen;
        private readonly string _folioTurno;
        private string? _rutaLogoEmpresa;
        private string _nombreAlmacen = string.Empty;
        private List<Dictionary<string, string>> _productos = new();

        public DocHojaConteoTurno(long idAlmacen, string folioTurno) {
            _idAlmacen = idAlmacen;
            _folioTurno = folioTurno;

            CargarInformacionEmpresa();

            string rutaLogo = !string.IsNullOrEmpty(_rutaLogoEmpresa) && File.Exists(_rutaLogoEmpresa)
                ? _rutaLogoEmpresa
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.png");

            if (File.Exists(rutaLogo)) CargarLogo(rutaLogo);
        }

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
                    string nombre = (!reader.IsDBNull("razon_social") &&
                                     !string.IsNullOrWhiteSpace(reader["razon_social"].ToString()))
                        ? reader["razon_social"].ToString()!
                        : reader["nombre"].ToString()!;

                    ConfigurarEmpresa(
                        nombre,
                        reader.IsDBNull("direccion") ? string.Empty : reader["direccion"].ToString()!,
                        reader.IsDBNull("telefono") ? string.Empty : reader["telefono"].ToString()!,
                        reader.IsDBNull("email") ? string.Empty : reader["email"].ToString()!,
                        reader.IsDBNull("web") ? string.Empty : reader["web"].ToString()!,
                        reader.IsDBNull("rif") ? string.Empty : reader["rif"].ToString()!
                    );

                    if (!reader.IsDBNull("ruta_logo"))
                        _rutaLogoEmpresa = reader["ruta_logo"].ToString();
                }
            } catch (Exception ex) {
                Debug.WriteLine($"[CargarInformacionEmpresa] Error: {ex.Message}");
            }
        }

        public override void GenerarDocumento(bool mostrar = true) {
            try {
                _productos = ObtenerInventarioAlmacen(_idAlmacen, out _nombreAlmacen);

                if (_productos.Count == 0) {
                    CentroNotificaciones.MostrarNotificacion(
                        "El almacén seleccionado no tiene productos activos con existencia registrada.",
                        Core.Modelos.Comun.TipoNotificacionEnum.Advertencia);
                    return;
                }

                var documento = new PdfDocument();
                documento.Info.Title = $"Hoja de Conteo de Turno — {_nombreAlmacen}";
                documento.Info.Author = NombreEmpresa;
                documento.Info.Creator = "aDVance ERP";

                var pagina = documento.AddPage();
                pagina.Size = PageSize.Letter;
                var gfx = XGraphics.FromPdfPage(pagina);

                var columnas = CrearColumnas(gfx);
                int paginaActual = 1;
                double yPos = DibujarCabeceraInventario(gfx, pagina, columnas);

                int fila = 0;

                foreach (var producto in _productos) {
                    if (NecesitaNuevaPagina(yPos, 22, gfx.PageSize.Height)) {
                        gfx.Dispose();

                        pagina = documento.AddPage();
                        pagina.Size = PageSize.Letter;
                        gfx = XGraphics.FromPdfPage(pagina);
                        paginaActual++;

                        yPos = DibujarCabeceraInventario(gfx, pagina, columnas);
                    }

                    fila++;

                    decimal precio = decimal.Parse(
                        producto["precio_venta_base"],
                        CultureInfo.InvariantCulture);

                    decimal esperado = decimal.Parse(
                        producto["cantidad_esperada"],
                        CultureInfo.InvariantCulture);

                    var datos = new Dictionary<string, (double ancho, string valor, XStringFormat formato)> {
                        ["Producto"] = (
                            columnas["Producto"],
                            TruncarTexto(producto["nombre"], FontContenido, gfx, columnas["Producto"] - 10),
                            XStringFormats.CenterLeft),

                        ["Inicio"] = (columnas["Inicio"], "", XStringFormats.CenterRight),
                        ["Entrada"] = (columnas["Entrada"], "", XStringFormats.Center),
                        ["Salida"] = (columnas["Salida"], "", XStringFormats.Center),
                        ["Disp."] = (columnas["Disp."], "", XStringFormats.Center),
                        ["Final"] = (columnas["Final"], "", XStringFormats.Center),
                        ["Venta"] = (columnas["Venta"], "", XStringFormats.Center),

                        ["Precio"] = (
                            columnas["Precio"],
                            "$" + precio.ToString("N2"),
                            XStringFormats.CenterRight),                        

                        ["Subtotal"] = (columnas["Subtotal"], "", XStringFormats.Center)
                    };

                    const double alturaFila = 18;

                    DibujarFilaTabla(
                        gfx,
                        yPos,
                        datos,
                        fila % 2 == 0,
                        alturaFila);

                    // Delimitación completa de celdas: la implementación base
                    // mantiene las líneas horizontales, pero deja las columnas
                    // del cuerpo sin separación vertical. Aquí completamos la
                    // cuadrícula respetando exactamente los anchos definidos
                    // para la tabla y el estándar gráfico del documento.
                    DibujarLineasColumnasTabla(
                        gfx,
                        yPos,
                        columnas,
                        alturaFila);

                    yPos += alturaFila;
                }

                // El cierre se mantiene como una sección independiente.
                // Si no cabe en la última página del inventario, se crea una nueva.
                const double alturaCierre = 430;

                if (NecesitaNuevaPagina(yPos, alturaCierre, gfx.PageSize.Height)) {
                    // No dibujar el pie aquí. Se agrega en un único pase final,
                    // cuando todos los XGraphics de todas las páginas ya fueron
                    // liberados y conocemos el total real de páginas.
                    gfx.Dispose();

                    pagina = documento.AddPage();
                    pagina.Size = PageSize.Letter;
                    gfx = XGraphics.FromPdfPage(pagina);
                    paginaActual++;

                    yPos = ObtenerInicioPosicionContenido() + 10;
                } else {
                    yPos += 15;
                }

                DibujarCuadreCierre(gfx, ref yPos, gfx.PageSize.Height);

                // MUY IMPORTANTE:
                // La última página todavía tiene un XGraphics activo. Debe
                // liberarse antes de abrir cualquier otro XGraphics sobre ella.
                gfx.Dispose();

                // El pie se dibuja una sola vez, después de conocer el total
                // definitivo de páginas.
                ActualizarNumeracionPaginas(documento, _nombreAlmacen);

                string rutaDocumento = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"HojaConteo_{_nombreAlmacen}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                documento.Save(rutaDocumento);

                if (mostrar) {
                    Process.Start(new ProcessStartInfo {
                        FileName = rutaDocumento,
                        UseShellExecute = true
                    });
                }

                CentroNotificaciones.MostrarNotificacion(
                    "Hoja de conteo generada exitosamente.",
                    Core.Modelos.Comun.TipoNotificacionEnum.Info);
            } catch (Exception ex) {
                CentroNotificaciones.MostrarNotificacion(
                    $"Error al generar la hoja de conteo: {ex.Message}",
                    Core.Modelos.Comun.TipoNotificacionEnum.Error);
            }
        }

        private Dictionary<string, double> CrearColumnas(XGraphics gfx) {
            double anchoDisponible = gfx.PageSize.Width - MargenIzquierdo - MargenDerecho;

            return new Dictionary<string, double> {
                ["Producto"] = anchoDisponible * 0.25,
                ["Inicio"] = anchoDisponible * 0.09,
                ["Entrada"] = anchoDisponible * 0.09,
                ["Salida"] = anchoDisponible * 0.08,
                ["Disp."] = anchoDisponible * 0.09,
                ["Final"] = anchoDisponible * 0.08,
                ["Venta"] = anchoDisponible * 0.09,
                ["Precio"] = anchoDisponible * 0.10,
                ["Subtotal"] = anchoDisponible * 0.15
            };
        }

        private double DibujarCabeceraInventario(
            XGraphics gfx,
            PdfPage pagina,
            Dictionary<string, double> columnas) {

            DibujarBannerProfesional(
                gfx,
                pagina,
                "CAMBIO DE TURNO",
                _nombreAlmacen,
                DateTime.Now);

            double yPos = ObtenerInicioPosicionContenido();
            double anchoDisponible = gfx.PageSize.Width - MargenIzquierdo - MargenDerecho;

            // El folio ya es conocido por el sistema; solo el responsable se
            // completa manualmente.
            double mitadCabecera = (anchoDisponible - 15) / 2;

            DibujarLineaCampo(
                gfx,
                MargenIzquierdo,
                yPos,
                mitadCabecera,
                $"Turno / Folio N.° {_folioTurno}");

            DibujarLineaCampo(
                gfx,
                MargenIzquierdo + mitadCabecera + 15,
                yPos,
                mitadCabecera,
                "Responsable");

            yPos += 30;

            // Nota separada del encabezado de tabla para evitar superposición.
            double alturaNota = 25;

            gfx.DrawRectangle(
                new XSolidBrush(ColorFondo),
                new XRect(
                    MargenIzquierdo,
                    yPos,
                    anchoDisponible,
                    alturaNota));

            gfx.DrawString(
                "En caso de existir diferencias al inicio del turno se documenta en las observaciones.",
                FontPequeno,
                new XSolidBrush(ColorTextoSecundario),
                new XRect(
                    MargenIzquierdo + 6,
                    yPos + 5,
                    anchoDisponible - 12,
                    alturaNota - 8),
                XStringFormats.CenterLeft);

            yPos += alturaNota + 8;

            DibujarEncabezadoTabla(gfx, yPos, columnas);
            yPos += 25;

            return yPos;
        }

        /// <summary>
        /// Completa la cuadrícula del cuerpo de la tabla dibujando las
        /// separaciones verticales de cada celda.
        ///
        /// Se utiliza una línea fina y neutra para mantener el estándar
        /// visual de DocumentoBase sin competir con el encabezado.
        /// </summary>
        private void DibujarLineasColumnasTabla(
            XGraphics gfx,
            double yPos,
            Dictionary<string, double> columnas,
            double alturaFila) {

            double xPos = MargenIzquierdo;

            var pen = new XPen(
                XColor.FromArgb(205, 205, 205),
                0.6);

            // Borde izquierdo.
            gfx.DrawLine(
                pen,
                MargenIzquierdo,
                yPos,
                MargenIzquierdo,
                yPos + alturaFila);

            foreach (var columna in columnas) {
                xPos += columna.Value;

                // Todas las separaciones verticales, incluida la derecha.
                gfx.DrawLine(
                    pen,
                    xPos,
                    yPos,
                    xPos,
                    yPos + alturaFila);
            }
        }

        private void DibujarCuadreCierre(
            XGraphics gfx,
            ref double yPos,
            double alturaPagina) {

            double anchoDisponible =
                gfx.PageSize.Width - MargenIzquierdo - MargenDerecho;

            // El cierre de caja es una sección independiente del inventario.
            // Si llega a esta página, el contenido se presenta como un bloque
            // físico de arqueo por denominaciones.
            gfx.DrawLine(
                new XPen(ColorSecundario, 2),
                MargenIzquierdo,
                yPos,
                gfx.PageSize.Width - MargenDerecho,
                yPos);

            yPos += 16;

            gfx.DrawString(
                "CIERRE DE CAJA",
                FontEncabezado,
                new XSolidBrush(ColorPrimario),
                new XPoint(MargenIzquierdo, yPos));

            yPos += 18;

            gfx.DrawString(
                "Conteo físico de efectivo",
                FontPequeno,
                new XSolidBrush(ColorTextoSecundario),
                new XPoint(MargenIzquierdo, yPos));

            yPos += 12;

            // Denominaciones solicitadas.
            double colDenominacion = anchoDisponible * 0.20;
            double colEsperado = anchoDisponible * 0.26;
            double colContado = anchoDisponible * 0.26;
            double colSubtotal = anchoDisponible * 0.28;

            var columnasCaja = new Dictionary<string, double> {
                ["Denominación"] = colDenominacion,
                ["Esperado"] = colEsperado,
                ["Contado"] = colContado,
                ["Subtotal"] = colSubtotal
            };

            DibujarEncabezadoTablaCaja(gfx, yPos, columnasCaja);
            yPos += 22;

            decimal[] denominaciones = {
                5000m, 2000m, 1000m, 500m, 200m,
                100m, 50m, 20m, 10m, 5m, 1m
            };

            int fila = 0;

            foreach (decimal denominacion in denominaciones) {
                const double alturaFila = 19;

                DibujarFilaCaja(
                    gfx,
                    yPos,
                    columnasCaja,
                    denominacion,
                    fila % 2 == 0,
                    alturaFila);

                yPos += alturaFila;
                fila++;
            }

            // Total de efectivo.
            const double alturaTotal = 21;

            DibujarFilaTotalCaja(
                gfx,
                yPos,
                columnasCaja,
                alturaTotal);

            yPos += alturaTotal + 15;

            // Extracciones y salario forman parte del cierre, pero quedan
            // fuera de la tabla de denominaciones para que puedan explicarse
            // independientemente.
            double mitad = (anchoDisponible - 15) / 2;

            DibujarLineaCampo(
                gfx,
                MargenIzquierdo,
                yPos,
                mitad,
                "Otras extracciones de caja");

            DibujarLineaCampo(
                gfx,
                MargenIzquierdo + mitad + 15,
                yPos,
                mitad,
                "Salario del vendedor");

            yPos += 30;

            gfx.DrawString(
                "TOTAL ENTREGADO",
                FontEncabezado,
                new XSolidBrush(ColorPrimario),
                new XPoint(MargenIzquierdo, yPos));

            yPos += 17;

            gfx.DrawLine(
                new XPen(ColorPrimario, 1.2),
                MargenIzquierdo,
                yPos,
                gfx.PageSize.Width - MargenDerecho,
                yPos);

            yPos += 20;

            gfx.DrawString(
                "ANOTACIONES DEL CIERRE",
                FontEncabezado,
                new XSolidBrush(ColorPrimario),
                new XPoint(MargenIzquierdo, yPos));

            yPos += 8;

            // Espacio amplio para diferencias, extracciones extraordinarias,
            // ajustes de apertura y cualquier otra incidencia.
            for (int i = 0; i < 6; i++) {
                yPos += 16;

                gfx.DrawLine(
                    new XPen(ColorTextoSecundario, 0.5),
                    MargenIzquierdo,
                    yPos,
                    gfx.PageSize.Width - MargenDerecho,
                    yPos);
            }

            yPos += 25;

            double mitadFirma = (anchoDisponible - 20) / 2;

            gfx.DrawLine(
                new XPen(ColorTextoSecundario, 0.8),
                MargenIzquierdo,
                yPos,
                MargenIzquierdo + mitadFirma,
                yPos);

            gfx.DrawLine(
                new XPen(ColorTextoSecundario, 0.8),
                MargenIzquierdo + mitadFirma + 20,
                yPos,
                MargenIzquierdo + 2 * mitadFirma + 20,
                yPos);

            yPos += 12;

            gfx.DrawString(
                "Firma del responsable de turno",
                FontPequeno,
                new XSolidBrush(ColorTextoSecundario),
                new XRect(
                    MargenIzquierdo,
                    yPos,
                    mitadFirma,
                    15),
                XStringFormats.TopCenter);

            gfx.DrawString(
                "Firma de quien transcribe / recibe",
                FontPequeno,
                new XSolidBrush(ColorTextoSecundario),
                new XRect(
                    MargenIzquierdo + mitadFirma + 20,
                    yPos,
                    mitadFirma,
                    15),
                XStringFormats.TopCenter);
        }

        private void DibujarEncabezadoTablaCaja(
            XGraphics gfx,
            double yPos,
            Dictionary<string, double> columnas) {

            double xPos = MargenIzquierdo;
            double altura = 22;

            gfx.DrawRectangle(
                new XSolidBrush(ColorPrimario),
                new XRect(
                    MargenIzquierdo,
                    yPos,
                    columnas.Values.Sum(),
                    altura));

            foreach (var columna in columnas) {
                gfx.DrawString(
                    columna.Key,
                    FontPequeno,
                    XBrushes.White,
                    new XRect(
                        xPos + 3,
                        yPos + 4,
                        columna.Value - 6,
                        altura - 6),
                    XStringFormats.Center);

                xPos += columna.Value;

                gfx.DrawLine(
                    new XPen(ColorFondo, 0.5),
                    xPos,
                    yPos,
                    xPos,
                    yPos + altura);
            }

            gfx.DrawLine(
                new XPen(ColorFondo, 0.5),
                MargenIzquierdo,
                yPos,
                MargenIzquierdo,
                yPos + altura);
        }

        private void DibujarFilaCaja(
            XGraphics gfx,
            double yPos,
            Dictionary<string, double> columnas,
            decimal denominacion,
            bool alterna,
            double alturaFila) {

            double ancho = columnas.Values.Sum();

            gfx.DrawRectangle(
                alterna
                    ? new XSolidBrush(ColorFondo)
                    : XBrushes.White,
                new XRect(
                    MargenIzquierdo,
                    yPos,
                    ancho,
                    alturaFila));

            var valores = new[] {
                "$" + denominacion.ToString("N0"),
                "",
                "",
                ""
            };

            double xPos = MargenIzquierdo;
            int indice = 0;

            foreach (var columna in columnas) {
                gfx.DrawString(
                    valores[indice],
                    FontContenido,
                    new XSolidBrush(ColorTexto),
                    new XRect(
                        xPos + 4,
                        yPos + 3,
                        columna.Value - 8,
                        alturaFila - 5),
                    indice == 0
                        ? XStringFormats.CenterLeft
                        : XStringFormats.CenterRight);

                xPos += columna.Value;

                gfx.DrawLine(
                    new XPen(XColor.FromArgb(205, 205, 205), 0.6),
                    xPos,
                    yPos,
                    xPos,
                    yPos + alturaFila);

                indice++;
            }

            gfx.DrawLine(
                new XPen(XColor.FromArgb(205, 205, 205), 0.6),
                MargenIzquierdo,
                yPos + alturaFila,
                MargenIzquierdo + ancho,
                yPos + alturaFila);

            gfx.DrawLine(
                new XPen(XColor.FromArgb(205, 205, 205), 0.6),
                MargenIzquierdo,
                yPos,
                MargenIzquierdo,
                yPos + alturaFila);
        }

        private void DibujarFilaTotalCaja(
            XGraphics gfx,
            double yPos,
            Dictionary<string, double> columnas,
            double alturaFila) {

            double ancho = columnas.Values.Sum();
            double xPos = MargenIzquierdo;

            gfx.DrawRectangle(
                new XSolidBrush(ColorFondo),
                new XRect(
                    MargenIzquierdo,
                    yPos,
                    ancho,
                    alturaFila));

            string[] valores = {
                "TOTAL EFECTIVO",
                "",
                "",
                ""
            };

            int indice = 0;

            foreach (var columna in columnas) {
                gfx.DrawString(
                    valores[indice],
                    FontContenido,
                    new XSolidBrush(ColorPrimario),
                    new XRect(
                        xPos + 4,
                        yPos + 3,
                        columna.Value - 8,
                        alturaFila - 5),
                    indice == 0
                        ? XStringFormats.CenterLeft
                        : XStringFormats.CenterRight);

                xPos += columna.Value;

                gfx.DrawLine(
                    new XPen(ColorPrimario, 0.7),
                    xPos,
                    yPos,
                    xPos,
                    yPos + alturaFila);

                indice++;
            }

            gfx.DrawLine(
                new XPen(ColorPrimario, 0.8),
                MargenIzquierdo,
                yPos,
                MargenIzquierdo + ancho,
                yPos);

            gfx.DrawLine(
                new XPen(ColorPrimario, 0.8),
                MargenIzquierdo,
                yPos + alturaFila,
                MargenIzquierdo + ancho,
                yPos + alturaFila);
        }

        private void ActualizarNumeracionPaginas(
            PdfDocument documento,
            string nombreAlmacen) {

            // Se redibuja únicamente el pie de cada página conservando todo
            // el contenido ya generado. Esto mantiene el estándar visual de
            // DocumentoBase y permite mostrar el total real de páginas.
            int totalPaginas = documento.Pages.Count;

            for (int i = 0; i < totalPaginas; i++) {
                var pagina = documento.Pages[i];

                // En este punto NO debe existir ningún XGraphics asociado a la
                // página. Cada contexto se crea, usa y destruye dentro de esta
                // iteración antes de continuar con la siguiente.
                using (var gfxPie = XGraphics.FromPdfPage(
                    pagina,
                    XGraphicsPdfPageOptions.Append)) {

                    DibujarPiePagina(
                        gfxPie,
                        pagina,
                        i + 1,
                        totalPaginas,
                        $"Hoja de conteo: {nombreAlmacen}");
                }
            }
        }

        private static List<Dictionary<string, string>> ObtenerInventarioAlmacen(
            long idAlmacen,
            out string nombreAlmacen) {

            var productos = new List<Dictionary<string, string>>();
            nombreAlmacen = string.Empty;

            using var connection =
                new MySqlConnection(ContextoBaseDatos.Configuracion.ToStringConexion());

            connection.Open();

            using (var cmdAlmacen = new MySqlCommand(
                "SELECT nombre FROM adv__almacen WHERE id_almacen = @id",
                connection)) {

                cmdAlmacen.Parameters.AddWithValue("@id", idAlmacen);
                var resultado = cmdAlmacen.ExecuteScalar();
                nombreAlmacen = resultado?.ToString() ?? "Almacén";
            }

            const string query = """
                SELECT p.nombre, p.precio_venta_base, i.cantidad AS cantidad_esperada
                FROM adv__inventario i
                INNER JOIN adv__producto p ON i.id_producto = p.id_producto
                WHERE i.id_almacen = @idAlmacen
                  AND p.activo = 1
                  AND p.es_vendible = 1
                ORDER BY p.nombre
                """;

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@idAlmacen", idAlmacen);

            using var reader = command.ExecuteReader();

            while (reader.Read()) {
                productos.Add(new Dictionary<string, string> {
                    ["nombre"] = reader["nombre"]?.ToString() ?? "",
                    ["precio_venta_base"] =
                        Convert.ToDecimal(reader["precio_venta_base"])
                            .ToString("N2", CultureInfo.InvariantCulture),
                    ["cantidad_esperada"] =
                        Convert.ToDecimal(reader["cantidad_esperada"])
                            .ToString("N2", CultureInfo.InvariantCulture)
                });
            }

            return productos;
        }

        private void DibujarLineaCampo(
            XGraphics gfx,
            double x,
            double y,
            double ancho,
            string etiqueta) {

            gfx.DrawString(
                etiqueta,
                FontPequeno,
                new XSolidBrush(ColorTextoSecundario),
                new XPoint(x, y));

            gfx.DrawLine(
                new XPen(ColorTextoSecundario, 0.6),
                x,
                y + 14,
                x + ancho,
                y + 14);
        }

        private string TruncarTexto(
            string texto,
            XFont fuente,
            XGraphics gfx,
            double anchoMaximo) {

            var tamano = gfx.MeasureString(texto, fuente);

            if (tamano.Width <= anchoMaximo)
                return texto;

            var textoTruncado = texto;

            while (textoTruncado.Length > 0) {
                textoTruncado = textoTruncado.Substring(0, textoTruncado.Length - 1);

                if (gfx.MeasureString(
                        textoTruncado + "...",
                        fuente).Width <= anchoMaximo) {

                    return textoTruncado + "...";
                }
            }

            return "...";
        }
    }
}

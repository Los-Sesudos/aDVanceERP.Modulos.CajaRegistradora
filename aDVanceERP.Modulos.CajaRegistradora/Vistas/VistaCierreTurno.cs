using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Modelos.Modulos.Comun;
using aDVanceERP.Core.Modelos.Modulos.Monedas;
using aDVanceERP.Core.Repositorios.Modulos.Caja;
using aDVanceERP.Core.Repositorios.Modulos.Monedas;
using aDVanceERP.Modulos.CajaRegistradora.Interfaces;

using Guna.UI2.WinForms;
using Guna.UI2.WinForms.Suite;

using System.Globalization;

namespace aDVanceERP.Modulos.CajaRegistradora.Vistas {
    public partial class VistaCierreTurno : Form, IVistaCierreTurno {
        private bool _modoEdicion = false;
        private CajaTurno? _turno = null!;
        private DateTime _fechaApertura;
        private decimal _montoApertura;
        private decimal _totalCalculado;
        private decimal _totalDeclarado;
        private decimal _totalDiferencia;

        public VistaCierreTurno() {
            InitializeComponent();

            NombreVista = nameof(VistaCierreTurno);

            Inicializar();
        }

        public bool ModoEdicion {
            get => _modoEdicion;
            set {
                _modoEdicion = value;

                fieldSubtitulo.Text = value ? "" : "Arqueo y conciliación de caja";
                btnConfirmarCierreTurno.Text = value ? "" : "Confirmar cierre";
            }
        }

        public string NombreVista {
            get => Name;
            set => Name = value;
        }

        public bool Habilitada {
            get => Enabled;
            set => Enabled = value;
        }

        public Point Coordenadas {
            get => Location;
            set => Location = value;
        }

        public Size Dimensiones {
            get => Size;
            set => Size = value;
        }

        public string CodigoTurno {
            get => fieldCodigo.Text;
            set {
                fieldCodigo.Text = value;

                _turno = RepoCajaTurno.Instancia.Buscar(FiltroBusquedaCajaTurno.Codigo, value).resultadosBusqueda.Select(r => r.entidadBase).FirstOrDefault();
            }
        }

        public string NombreAlmacen {
            get => fieldAlmacen.Text;
            set => fieldAlmacen.Text = value;
        }

        public DateTime FechaApertura {
            get => _fechaApertura;
            set {
                _fechaApertura = value;

                fieldFechaHoraApertura.Text = value.ToString("dd/MM/yyyy HH:mm");
            }
        }

        public decimal MontoApertura {
            get => _montoApertura;
            set {
                _montoApertura = value;

                fieldFondoInicial.Text = value.ToString("N2", CultureInfo.InvariantCulture);
            }
        }

        public decimal TotalCalculado {
            get => _totalCalculado;
            set {
                _totalCalculado = value;
            }
        }

        public decimal TotalDeclarado {
            get => _totalDeclarado;
            set {
                _totalDeclarado = value;
            }
        }

        public decimal TotalDiferencia {
            get => _totalDiferencia;
            set {
                _totalDiferencia = value;
            }
        }

        public string? Observaciones { get; set; }
        //    get => fieldObservaciones.Text;
        //    set => fieldObservaciones.Text = value;
        //}

        public event EventHandler? RegistrarEntidad;
        public event EventHandler? EditarEntidad;
        public event EventHandler? EliminarEntidad;
        public event EventHandler? ConciliacionModificada;
        public event EventHandler? ConfirmarCierreTurno;

        public void Inicializar() {
            btnConfirmarCierreTurno.Click += delegate (object? sender, EventArgs args) {
                ConfirmarCierreTurno?.Invoke(sender, args);
            };
            btnSalir.Click += delegate (object? sender, EventArgs args) {
                Ocultar();
            };
        }

        public void Mostrar() {
            BringToFront();
            Show();
        }

        public void Ocultar() {
            Hide();
        }

        public void Restaurar() {
            fieldOperador.Text = ContextoSeguridad.UsuarioAutenticado?.Nombre ?? "admin";
        }

        public void Cerrar() {
            Dispose();
        }

        public IEnumerable<CajaArqueo> ObtenerArqueo() {
            var arqueo = new List<CajaArqueo>();
            var repoMoneda = RepoMoneda.Instancia;
            var repoTasaCambio = RepoTasaCambio.Instancia;
            var idMonedaBase = repoMoneda.ObtenerMonedaBase().Id;

            foreach (var control in panelConteoDenominaciones.Controls) {
                if (control is VistaTuplaConteoFisicoDenominacion tupla && tupla.Conteo > 0) {
                    var monedaOrigen = repoMoneda
                        .Buscar(FiltroBusquedaMoneda.Codigo, tupla.Tag?.ToString() ?? "")
                        .resultadosBusqueda
                        .FirstOrDefault()
                        .entidadBase;

                    arqueo.Add(new CajaArqueo {
                        IdTurno = _turno?.Id ?? 0,
                        Denominacion = tupla.ValorDenominacion,
                        Cantidad = tupla.Conteo,
                        IdMoneda = monedaOrigen?.Id ?? 1,
                        TasaCambioAplicada = repoTasaCambio.ObtenerTasaVigente(monedaOrigen?.Id ?? idMonedaBase, idMonedaBase, _turno?.FechaApertura ?? DateTime.Today)
                    });
                }
            }

            return arqueo;
        }

        public void PopularDenominaciones(DenominacionMoneda[] denominaciones) {
            var repoMoneda = RepoMoneda.Instancia;
            var monedas = new List<Moneda>();

            foreach (var denominacion in denominaciones) {
                // Botón de denominación
                if (!monedas.Exists(m => m.Id.Equals(denominacion.IdMoneda))) {
                    var moneda = repoMoneda.ObtenerPorId(denominacion.IdMoneda)!;

                    AdicionarBotonMoneda(moneda);

                    monedas.Add(moneda);
                }

                // Denominaciones
                AdicionarConteoDenominacion(denominacion, monedas.First(m => m.Id.Equals(denominacion.IdMoneda)));
            }

            repoMoneda.Dispose();
            monedas.Clear();

            // Presionar el primer botón de monedas
            if (panelMonedasDenominaciones.Controls.Count > 0)
                (panelMonedasDenominaciones.Controls[0] as Guna2Button)?.PerformClick();
        }

        private void AdicionarBotonMoneda(Moneda moneda) {
            if (panelConteoDenominaciones.Controls.ContainsKey($"btnMoneda{moneda.Codigo}"))
                return;

            var btnMoneda = new Guna2Button();
            var customizableEdges = new CustomizableEdges();

            btnMoneda.Animated = true;
            btnMoneda.BackColor = Color.White;
            btnMoneda.ButtonMode = Guna.UI2.WinForms.Enums.ButtonMode.RadioButton;
            btnMoneda.CheckedState.FillColor = Color.White;
            btnMoneda.CheckedState.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            btnMoneda.CustomImages.CheckedImage = Properties.Resources.barra_seleccion;
            btnMoneda.CustomImages.ImageAlign = HorizontalAlignment.Center;
            btnMoneda.CustomImages.ImageOffset = new Point(0, 27);
            btnMoneda.CustomImages.ImageSize = new Size(131, 8);
            btnMoneda.CustomizableEdges = customizableEdges;
            btnMoneda.FillColor = Color.White;
            btnMoneda.Font = new Font("Segoe UI", 11.25F);
            btnMoneda.ForeColor = Color.Black;
            btnMoneda.Location = new Point(0, 0);
            btnMoneda.Margin = new Padding(0);
            btnMoneda.Name = $"btnMoneda{moneda.Codigo}";
            btnMoneda.ShadowDecoration.CustomizableEdges = customizableEdges;
            btnMoneda.Size = new Size(140, panelMonedasDenominaciones.Height);
            btnMoneda.TabIndex = panelMonedasDenominaciones.Controls.Count;
            btnMoneda.Tag = moneda;
            btnMoneda.Text = moneda.Codigo;

            // Eventos
            btnMoneda.Click += MostrarConteoDenominacionesMoneda;

            panelMonedasDenominaciones.Controls.Add(btnMoneda);
        }

        private void AdicionarConteoDenominacion(DenominacionMoneda denominacion, Moneda moneda) {
            var tuplaConteoFisicoDenominacion = new VistaTuplaConteoFisicoDenominacion();

            tuplaConteoFisicoDenominacion.Moneda = moneda;
            tuplaConteoFisicoDenominacion.Simbolo = moneda.Simbolo;
            tuplaConteoFisicoDenominacion.ValorDenominacion = (int) denominacion.Valor;

            tuplaConteoFisicoDenominacion.Name = $"tupla{(int) denominacion.Valor}{moneda.Codigo}";
            tuplaConteoFisicoDenominacion.Width = panelConteoDenominaciones.Width / 2 - 7;
            tuplaConteoFisicoDenominacion.Tag = moneda.Codigo;
            tuplaConteoFisicoDenominacion.TopLevel = false;
            tuplaConteoFisicoDenominacion.Hide();

            // Eventos
            tuplaConteoFisicoDenominacion.ConteoDenominacionActualizado += OnConteoDenominacionActualizado;

            panelConteoDenominaciones.Controls.Add(tuplaConteoFisicoDenominacion);
        }

        private void MostrarConteoDenominacionesMoneda(object? sender, EventArgs e) {
            var btnMoneda = sender as Guna2Button;
            var moneda = btnMoneda!.Tag as Moneda;
            var conteoFisicoDenominacion = 0m;

            foreach (var control in panelConteoDenominaciones.Controls) {
                if (control is VistaTuplaConteoFisicoDenominacion tuplaConteoFisicoDenominacion)
                    if (tuplaConteoFisicoDenominacion.Name.EndsWith(moneda!.Codigo)) {
                        tuplaConteoFisicoDenominacion.Visible = true;
                        conteoFisicoDenominacion += tuplaConteoFisicoDenominacion.Total;
                    } else tuplaConteoFisicoDenominacion.Visible = false;
            }

            fieldTotalContado.Text = conteoFisicoDenominacion.ToString("N2", CultureInfo.InvariantCulture);
        }

        private void OnConteoDenominacionActualizado(object? sender, EventArgs e) {
            ActualizarTotalContado(sender as VistaTuplaConteoFisicoDenominacion);
        }

        private void ActualizarTotalContado(VistaTuplaConteoFisicoDenominacion? t) {
            var codigo = t?.Tag?.ToString() ?? string.Empty;
            var conteoFisicoDenominacion = 0m;

            foreach (var control in panelConteoDenominaciones.Controls) {
                if (control is VistaTuplaConteoFisicoDenominacion tuplaConteoFisicoDenominacion)
                    if (tuplaConteoFisicoDenominacion.Name.EndsWith(codigo))
                        conteoFisicoDenominacion += tuplaConteoFisicoDenominacion.Total;
            }

            fieldTotalContado.Text = conteoFisicoDenominacion.ToString("N2", CultureInfo.InvariantCulture);

            foreach (var control in panelConciliacion.Controls) {
                if (control is VistaTuplaConciliacionMoneda tupla 
                        && tupla.MonedaCanal.moneda.Codigo == codigo
                        && tupla.MonedaCanal.canalPago == CanalPagoEnum.Efectivo) {
                    tupla.MontoDeclarado = conteoFisicoDenominacion;
                    break;
                }
            }
        }

        public List<CajaConciliacionMoneda> ObtenerConciliacion() {
            var resultado = new List<CajaConciliacionMoneda>();

            foreach (var control in panelConciliacion.Controls) {
                if (control is VistaTuplaConciliacionMoneda tupla) {
                    resultado.Add(new CajaConciliacionMoneda {
                        IdTurno = _turno?.Id ?? 0,
                        IdMoneda = tupla.MonedaCanal.moneda.Id,
                        CanalPago = tupla.MonedaCanal.canalPago,
                        MontoCalculado = tupla.MontoCalculado,
                        MontoDeclarado = tupla.MontoDeclarado,
                        Diferencia = tupla.Diferencia
                    });
                }
            }

            return resultado;
        }

        public void PopularConciliacion(List<TotalesCierreCaja> totalesCalculados, long idMonedaBase) {
            panelConciliacion.Controls.Clear();

            var repoMoneda = RepoMoneda.Instancia;
            
            foreach (var calculado in totalesCalculados) {
                var moneda = repoMoneda.ObtenerPorId(calculado.IdMoneda) ?? new Moneda { Codigo = "N/A", Simbolo = "$" };

                AgregarFilaConciliacion(moneda, CanalPagoEnum.Efectivo,
                    calculado.TotalEfectivo, declarado: 0, editable: false);

                // Solo la moneda base concilia transferencia bancaria — el resto es efectivo únicamente.
                if (calculado.IdMoneda == idMonedaBase) {
                    AgregarFilaConciliacion(moneda, CanalPagoEnum.TransferenciaBancaria,
                        calculado.TotalTransferencias, calculado.TotalTransferencias, editable: true);
                }
            }

            ActualizarTotalesGenerales();
        }

        private void AgregarFilaConciliacion(Moneda moneda, CanalPagoEnum canal, decimal calculado, decimal declarado, bool editable) {
            var tupla = new VistaTuplaConciliacionMoneda {
                MonedaCanal = (moneda, canal),
                MontoCalculado = calculado,
                MontoDeclarado = declarado,
                Diferencia = declarado - calculado,
                DeclaradoEditable = editable,
                Name = $"conciliacion{moneda.Codigo}{canal}",
                Dock = DockStyle.Top,
                TopLevel = false
            };

            // Eventos
            tupla.MontoDeclaradoModificado += (s, e) => {
                tupla.Diferencia = tupla.MontoDeclarado - tupla.MontoCalculado;
                ActualizarTotalesGenerales();
                ConciliacionModificada?.Invoke(this, EventArgs.Empty);
            };

            panelConciliacion.Controls.Add(tupla);
            tupla.Mostrar();
        }

        #region AUXILIARES

        private void ActualizarTotalesGenerales() {
            var repoTasaCambio = RepoTasaCambio.Instancia;
            var repoMoneda = RepoMoneda.Instancia;
            var monedaBase = repoMoneda.ObtenerMonedaBase();
            var simboloBase = monedaBase.Simbolo;

            // Limpiar totales
            TotalCalculado = 0;
            TotalDeclarado = 0;
            TotalDiferencia = 0;

            foreach (var control in panelConciliacion.Controls) {
                if (control is VistaTuplaConciliacionMoneda tupla) {
                    TotalCalculado += repoTasaCambio.Convertir(tupla.MontoCalculado, tupla.MonedaCanal.moneda.Id, monedaBase.Id, FechaApertura);
                    TotalDeclarado += repoTasaCambio.Convertir(tupla.MontoDeclarado, tupla.MonedaCanal.moneda.Id, monedaBase.Id, FechaApertura);
                }
            }

            fieldTotalCalculado.Text = $"{simboloBase} {TotalCalculado:N2}";
            fieldTotalDeclarado.Text = $"{simboloBase} {TotalDeclarado:N2}";

            TotalDiferencia = TotalDeclarado - TotalCalculado;
            
            fieldTotalDiferencia.Text = $"{simboloBase} {TotalDiferencia:N2}";

            var colorDiferencia = TotalDiferencia < 0
                    ? Color.FromArgb(198, 40, 40) // Rojo para diferencia negativa (falta dinero)
                    : TotalDiferencia > 0
                        ? Color.FromArgb(255, 193, 7) // Ámbar para diferencia positiva (sobrante de dinero)
                        : Color.FromArgb(46, 125, 50); // Verde para sin diferencia (balanceado)

            fieldTituloTotalDiferencia.ForeColor = colorDiferencia;
            fieldTotalDiferencia.ForeColor = colorDiferencia;
        }

        #endregion
    }
}

using aDVanceERP.Core.Infraestructura.Extensiones.Comun;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Modelos.Modulos.Comun;
using aDVanceERP.Core.Modelos.Modulos.Monedas;
using aDVanceERP.Modulos.CajaRegistradora.Interfaces;

using System.Globalization;

namespace aDVanceERP.Modulos.CajaRegistradora.Vistas {
    public partial class VistaMovimientoCaja : Form, IVistaMovimientoCaja {
        private bool _modoEdicion = false;
        private string _codigo = string.Empty;
        private string _nombreAlmacen = string.Empty;

        public VistaMovimientoCaja() {
            InitializeComponent();

            NombreVista = nameof(VistaMovimientoCaja);

            Inicializar();
        }

        public bool ModoEdicion {
            get => _modoEdicion;
            set {
                _modoEdicion = value;

                fieldSubtitulo.Text = value ? "" : $"Turno _ · _";
                btnRegistrarActualizar.Text = value ? "" : "Registrar movimiento";
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

        public string Codigo {
            get => _codigo;
            set {
                _codigo = value;
                fieldSubtitulo.Text = $"Turno {_codigo} · {_nombreAlmacen}";
            }
        }

        public long IdAlmacen { get; set; }

        public string NombreAlmacen {
            get => _nombreAlmacen;
            set {
                _nombreAlmacen = value;
                fieldSubtitulo.Text = $"Turno {_codigo} · {_nombreAlmacen}";
            }
        }

        public TipoMovimientoCajaEnum Tipo { get; set; }



        public CanalPagoEnum CanalPago {
            get => (CanalPagoEnum) fieldCanalPago.SelectedIndex;
            set => fieldCanalPago.SelectedItem = value.ObtenerNombreDescripcion();
        }

        public decimal Monto {
            get => decimal.TryParse(fieldMonto.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var monto) ? monto : 0m;
            set => fieldMonto.Text = value.ToString("N2", CultureInfo.InvariantCulture);
        }

        public Moneda? MonedaPago {
            get => fieldMonedaPago.SelectedItem as Moneda;
            set {
                fieldMonedaPago.SelectedItem = value;
                CambioMonedaPago?.Invoke(this, value?.Id ?? 0);
            }
        }

        public string? Descripcion {
            get => fieldDescripcion.Text;
            set => fieldDescripcion.Text = value;
        }

        public event EventHandler? RegistrarEntidad;
        public event EventHandler? EditarEntidad;
        public event EventHandler? EliminarEntidad;
        public event EventHandler<long>? CambioMonedaPago;

        public void Inicializar() {
            fieldTipoMovimiento.CheckedChanged += delegate {
                fieldTipoMovimiento.Text = fieldTipoMovimiento.Checked
                    ? "↓  Entrada manual"
                    : "↑  Salida manual";
                fieldTipoMovimiento.FillColor = fieldTipoMovimiento.Checked
                    ? Color.FromArgb(232, 245, 233)
                    : Color.FromArgb(252, 228, 236);
                fieldTipoMovimiento.BorderColor = fieldTipoMovimiento.Checked
                    ? Color.FromArgb(129, 199, 132)
                    : Color.FromArgb(239, 154, 154);
                fieldTipoMovimiento.ForeColor = fieldTipoMovimiento.Checked
                    ? Color.FromArgb(46, 125, 50)
                    : Color.FromArgb(198, 40, 40);
                Tipo = fieldTipoMovimiento.Checked
                    ? TipoMovimientoCajaEnum.EntradaManual
                    : TipoMovimientoCajaEnum.SalidaManual;
            };
            btnRegistrarActualizar.Click += delegate (object? sender, EventArgs args) {
                RegistrarEntidad?.Invoke(sender, args);
            };
            btnSalir.Click += delegate (object? sender, EventArgs args) { Ocultar(); };
        }

        public void Mostrar() {
            BringToFront();
            Show();
        }

        public void Ocultar() {
            Hide();
        }

        public void Restaurar() {
            IdAlmacen = 0L;
            NombreAlmacen = string.Empty;
            fieldTipoMovimiento.Checked = true;
            CanalPago = CanalPagoEnum.Efectivo;
            Descripcion = string.Empty;

            fieldMonto.Text = string.Empty;
            fieldMonedaPago.SelectedIndex = 0;
        }

        public void Cerrar() {
            Dispose();
        }

        public void CargarMetodosPago(string[] metodosPago) {
            fieldCanalPago.Items.Clear();
            fieldCanalPago.Items.AddRange([.. metodosPago.SkipLast(2)]);
            fieldCanalPago.SelectedIndex = 0;
        }

        public void CargarMonedasPago(Moneda[] monedas) {
            fieldMonedaPago.Items.Clear();
            fieldMonedaPago.Items.AddRange(monedas);
            fieldMonedaPago.SelectedItem = monedas.Length > 0 ? 0 : -1;
        }
    }
}

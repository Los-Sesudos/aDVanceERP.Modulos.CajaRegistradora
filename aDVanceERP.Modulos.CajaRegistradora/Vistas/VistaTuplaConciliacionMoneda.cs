using aDVanceERP.Core.Infraestructura.Extensiones.Comun;
using aDVanceERP.Core.Modelos.Modulos.Comun;
using aDVanceERP.Core.Modelos.Modulos.Monedas;

namespace aDVanceERP.Modulos.CajaRegistradora.Vistas {
    public partial class VistaTuplaConciliacionMoneda : Form {
        private Moneda _moneda { get; set; }
        private CanalPagoEnum _canalPago { get; set; }

        public VistaTuplaConciliacionMoneda() {
            InitializeComponent();
            Inicializar();
        }       

        public (Moneda moneda, CanalPagoEnum canalPago) MonedaCanal {
            get => (_moneda, _canalPago);
            set {
                _moneda = value.moneda;
                _canalPago = value.canalPago;
                                
                fieldTituloMonedaCanal.Text = $"{value.moneda.Codigo} ({value.canalPago.ObtenerNombreDescripcion().Nombre})";
            }
        }

        public decimal MontoCalculado {
            get => decimal.TryParse(fieldMontoCalculado.Text.Replace(_moneda.Simbolo, string.Empty).Trim(), out var v) ? v : 0m;
            set => fieldMontoCalculado.Text = $"{_moneda.Simbolo} {value:N2}";
        }

        public decimal MontoDeclarado {
            get => decimal.TryParse(fieldMontoDeclarado.Text, out var v) ? v : 0m;
            set => fieldMontoDeclarado.Text = value.ToString("N2");
        }

        public decimal Diferencia {
            get => decimal.TryParse(fieldDiferencia.Text.Replace(_moneda.Simbolo, string.Empty).Trim(), out var v) ? v : 0m;
            set {
                fieldDiferencia.Text = $"{_moneda.Simbolo} {value:N2}";
                fieldDiferencia.ForeColor = value < 0
                    ? Color.FromArgb(198, 40, 40)
                    : value > 0
                        ? Color.FromArgb(255, 193, 7)
                        : Color.FromArgb(46, 125, 50);
            }
        }

        /// <summary>El campo declarado solo se habilita para efectivo — una transferencia
        /// bancaria se concilia contra el estado de cuenta, no se "cuenta" a mano.</summary>
        public bool DeclaradoEditable {
            get => !fieldMontoDeclarado.ReadOnly;
            set => fieldMontoDeclarado.ReadOnly = !value;
        }

        public event EventHandler? MontoDeclaradoModificado;

        public void Inicializar() {
            fieldMontoDeclarado.TextChanged += (s, e) => MontoDeclaradoModificado?.Invoke(this, EventArgs.Empty);
        }

        public void Mostrar() { TopLevel = false; Show(); }
        public void Cerrar() { Dispose(); }
    }
}
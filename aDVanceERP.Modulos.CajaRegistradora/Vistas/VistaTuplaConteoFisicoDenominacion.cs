using aDVanceERP.Core.Modelos.Modulos.Monedas;

namespace aDVanceERP.Modulos.CajaRegistradora.Vistas {
    public partial class VistaTuplaConteoFisicoDenominacion : Form {
        private int _valorDenominacion;
        private int _cantidad;
        private string _simbolo = "$";

        public VistaTuplaConteoFisicoDenominacion() {
            InitializeComponent();

            fieldConteoDenominacion.Leave += OnCambioConteoDenominacion;
            Disposed += (s, e) => {
                fieldConteoDenominacion.Leave -= OnCambioConteoDenominacion;
            };
        }

        public event EventHandler? ConteoDenominacionActualizado;

        public Moneda Moneda { get; set; }

        public string Simbolo { 
            get => _simbolo; 
            set { 
                _simbolo = value;
                fieldTituloDenominacion.Text = $"{Simbolo} {_valorDenominacion:N0}";
                fieldMontoTotal.Text = $"= {Simbolo} 0";
            }
        }

        public int ValorDenominacion { 
            get => _valorDenominacion; 
            set { 
                _valorDenominacion = value; 
                fieldTituloDenominacion.Text = $"{Simbolo} {_valorDenominacion:N0}";
                fieldMontoTotal.Text = $"= {Simbolo} 0";
            } 
        }
        
        public int Conteo {
            get => _cantidad;
            set {
                _cantidad = value;
                fieldConteoDenominacion.Text = _cantidad.ToString();
                fieldMontoTotal.Text = $"= {Simbolo} {Total:N0}";
            }
        }

        public int Total => ValorDenominacion * Conteo;

        private void OnCambioConteoDenominacion(object? sender, EventArgs e) {
            if (int.TryParse(fieldConteoDenominacion.Text, out int cantidad)) {
                Conteo = cantidad;
            } else {
                fieldMontoTotal.Text = $"= {Simbolo} 0";
            }

            ConteoDenominacionActualizado?.Invoke(this, EventArgs.Empty);
        }
    }
}

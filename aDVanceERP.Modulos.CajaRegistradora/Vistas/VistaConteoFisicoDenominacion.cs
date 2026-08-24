using System.Globalization;

namespace aDVanceERP.Modulos.CajaRegistradora.Vistas {
    public partial class VistaConteoFisicoDenominacion : Form {
        private int _valorDenominacion;
        private int _cantidad;

        public VistaConteoFisicoDenominacion() {
            InitializeComponent();

            fieldConteoDenominacion.TextChanged += OnCambioConteoDenominacion;
            Disposed += (s, e) => {
                fieldConteoDenominacion.TextChanged -= OnCambioConteoDenominacion;
            };
        }

        public string Simbolo { get; set; } = "$";
        
        public int ValorDenominacion { 
            get => _valorDenominacion; 
            set { 
                _valorDenominacion = value; 
                fieldTituloDenominacion.Text = $"{Simbolo} {_valorDenominacion:N0}";
            } 
        }
        
        public int Conteo {
            get => _cantidad;
            set {
                _cantidad = value;
                fieldConteoDenominacion.Text = _cantidad.ToString();
                fieldMontoTotal.Text = $"{Simbolo} {Total:N0}";
            }
        }

        public int Total => ValorDenominacion * Conteo;

        private void OnCambioConteoDenominacion(object? sender, EventArgs e) {
            if (int.TryParse(fieldConteoDenominacion.Text, out int cantidad)) {
                Conteo = cantidad;
            } else {
                fieldMontoTotal.Text = $"{Simbolo} 0";
            }
        }
    }
}

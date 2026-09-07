using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Modelos.Modulos.Inventario;
using aDVanceERP.Modulos.CajaRegistradora.Interfaces;

using System.Globalization;

namespace aDVanceERP.Modulos.CajaRegistradora.Vistas {
    public partial class VistaAperturaTurno : Form, IVistaAperturaTurno {
        private bool _modoEdicion = false;

        public VistaAperturaTurno() {
            InitializeComponent();

            NombreVista = nameof(VistaAperturaTurno);

            Inicializar();
        }

        public bool ModoEdicion {
            get => _modoEdicion;
            set {
                _modoEdicion = value;

                fieldSubtitulo.Text = value ? "" : "Registro de fondo inicial de caja";
                btnRegistrarActualizar.Text = value ? "" : "Abrir turno";
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

        public Almacen? Almacen {
            get => fieldAlmacen.SelectedItem as Almacen;
            set => fieldAlmacen.SelectedItem = value;
        }

        public DateTime FechaApertura {
            get => fieldFechaApertura.Value;
            set => fieldFechaApertura.Value = value;
        }

        public decimal MontoApertura {
            get => decimal.TryParse(fieldMontoEfectivo.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var monto) ? monto : 0m;
            set => fieldMontoEfectivo.Text = value.ToString("N2", CultureInfo.InvariantCulture);
        }

        public string? Observaciones {
            get => fieldObservaciones.Text;
            set => fieldObservaciones.Text = value;
        }

        public event EventHandler? RegistrarEntidad;
        public event EventHandler? EditarEntidad;
        public event EventHandler? EliminarEntidad;

        public void Inicializar() {
            fieldFechaApertura.Value = DateTime.Now;
            btnRegistrarActualizar.Click += delegate (object? sender, EventArgs args) {
                RegistrarEntidad?.Invoke(sender, args);
            };
            btnSalir.Click += delegate (object? sender, EventArgs args) { Ocultar(); };
        }

        public void Mostrar() {
            fieldOperador.Text = ContextoSeguridad.UsuarioAutenticado?.Nombre;

            BringToFront();
            Show();
        }

        public void Ocultar() {
            Hide();
        }

        public void Restaurar() {
            fieldMontoEfectivo.Text = string.Empty;
            Observaciones = string.Empty;

            // Resetear selección de almacén al primero disponible
            if (fieldAlmacen.Items.Count > 0)
                fieldAlmacen.SelectedIndex = 0;
        }

        public void Cerrar() {
            Dispose();
        }

        public void CargarAlmacenes(Almacen[] almacenes) {
            fieldAlmacen.Items.Clear();
            fieldAlmacen.Items.Add("— Seleccione un almacén —");
            fieldAlmacen.Items.AddRange(almacenes);
            fieldAlmacen.SelectedIndex = 0;
        }
    }
}

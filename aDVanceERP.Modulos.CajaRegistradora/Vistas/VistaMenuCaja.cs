using aDVanceERP.Core.Eventos;
using aDVanceERP.Core.Eventos.Comun;
using aDVanceERP.Core.Eventos.Modulos.Caja;
using aDVanceERP.Modulos.CajaRegistradora.Interfaces;

namespace aDVanceERP.Modulos.CajaRegistradora.Vistas {
    public partial class VistaMenuCaja : Form, IVistaMenuCajaRegistradora {
        public VistaMenuCaja() {
            InitializeComponent();

            NombreVista = nameof(VistaMenuCaja);

            Inicializar();
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
    
        public void Inicializar() {
            // Eventos
            btnCaja.Click += delegate { AgregadorEventos.Publicar(new EventoMostrarVistaGestionCaja()); };
        }

        public void SeleccionarVistaInicial() {
            if (btnCaja.Visible)
                btnCaja.PerformClick();
        }

        public void Mostrar() {
            BringToFront();
            Show();
        }

        public void Restaurar() {
            btnCaja.Checked = false;
        }

        public void Ocultar() {
            Hide();
        }

        public void Cerrar() {
            Dispose();
        }
    }
}
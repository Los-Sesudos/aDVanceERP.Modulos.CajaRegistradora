using aDVanceERP.Core.Eventos.Comun;
using aDVanceERP.Core.Eventos.Modulos.Caja;
using aDVanceERP.Modulos.CajaRegistradora.Interfaces;

namespace aDVanceERP.Modulos.CajaRegistradora {
    public partial class VistaMenuMaestros : Form, IVistaMenuMaestros {
        public VistaMenuMaestros() {
            InitializeComponent();

            NombreVista = $"{nameof(VistaMenuMaestros)}CajaRegistradora";

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
            btnAtras.Click += delegate { 
                AgregadorEventos.Publicar(new EventoMostrarVistaMenuCajaRegistradora()); 
            };
        }

        public void SeleccionarVistaInicial() {
        }

        public void Mostrar() {
            BringToFront();
            Show();
        }

        public void Restaurar() {
            btnMonedas.Checked = false;
        }

        public void Ocultar() {
            Hide();
        }

        public void Cerrar() {
            Dispose();
        }
    }
}
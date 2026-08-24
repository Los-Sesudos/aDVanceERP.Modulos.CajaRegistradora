using aDVanceERP.Core.Infraestructura.Globales;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Repositorios.Modulos.Caja;
using aDVanceERP.Modulos.CajaRegistradora.Interfaces;

using System.Globalization;

namespace aDVanceERP.Modulos.CajaRegistradora.Vistas {
    public partial class VistaCierreTurno : Form, IVistaCierreTurno {
        private bool _modoEdicion = false;
        private CajaTurno? _turno = null!;
        private List<CajaArqueo> _arqueosCajaBd = null!;
        private DateTime _fechaApertura;
        private decimal _montoApertura;
        private decimal _totalEfectivoCalculado;
        private decimal _totalTransferenciasCalculado;
        private decimal _montoEfectivoDeclarado;
        private decimal _montoTransferenciasDeclarado;
        private decimal _diferenciaEfectivo;
        private decimal _diferenciaTransferencias;

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
                _arqueosCajaBd = RepoCajaArqueo.Instancia.ObtenerPorTurno(_turno?.Id ?? 0);
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

        public decimal TotalEfectivoCalculado {
            get => _totalEfectivoCalculado;
            set {
                _totalEfectivoCalculado = value;

                fieldEfectivoCalculado.Text = $"$ {value:N2}";
            }
        }

        public decimal TotalTransferenciasCalculado {
            get => _totalTransferenciasCalculado;
            set {
                _totalTransferenciasCalculado = value;

                //fieldTransferenciaCalculada.Text = $"$ {value:N2}";
            }
        }

        public decimal MontoEfectivoDeclarado {
            get => _montoEfectivoDeclarado;
            set {
                _montoEfectivoDeclarado = value;

                fieldMontoEfectivoDeclarado.Text = value > 0
                    ? value.ToString("N2", CultureInfo.InvariantCulture)
                    : string.Empty;
            }
        }

        public decimal MontoTransferenciasDeclarado {
            get => _montoTransferenciasDeclarado;
            set {
                _montoTransferenciasDeclarado = value;

                //fieldMontoTransferenciaDeclarado.Text = value > 0 
                //    ? value.ToString("N2", CultureInfo.InvariantCulture)
                //    : string.Empty;
            }
        }

        public decimal DiferenciaEfectivo {
            get => _diferenciaEfectivo;
            set {
                _diferenciaEfectivo = value;

                fieldDiferenciaEfectivo.Text = $"$ {value:N2}";
                fieldDiferenciaEfectivo.ForeColor = value < 0
                    ? Color.FromArgb(198, 40, 40) // Rojo para diferencia negativa (falta dinero)
                    : value > 0
                        ? Color.FromArgb(255, 193, 7) // Ámbar para diferencia positiva (sobrante de dinero)
                        : Color.FromArgb(46, 125, 50); // Verde para sin diferencia (balanceado)
            }
        }

        public decimal DiferenciaTransferencias {
            get => _diferenciaTransferencias;
            set {
                _diferenciaTransferencias = value;

                //fieldDiferenciaTransferencias.Text = $"$ {value:N2}";
                //fieldDiferenciaTransferencias.ForeColor = value < 0
                //    ? Color.FromArgb(198, 40, 40) // Rojo para diferencia negativa (falta dinero)
                //    : value > 0
                //        ? Color.FromArgb(255, 193, 7) // Ámbar para diferencia positiva (sobrante de dinero)
                //        : Color.FromArgb(46, 125, 50); // Verde para sin diferencia (balanceado)
            }
        }

        public string? Observaciones { get; set; }
        //    get => fieldObservaciones.Text;
        //    set => fieldObservaciones.Text = value;
        //}

        public event EventHandler? RegistrarEntidad;
        public event EventHandler? EditarEntidad;
        public event EventHandler? EliminarEntidad;
        public event EventHandler? ArqueoModificado;
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
            return [];
        }

        public void ActualizarTotalArqueo(decimal totalContado) {
            fieldTotalContado.Text = $"$ {totalContado:N2}";
        }
    }
}

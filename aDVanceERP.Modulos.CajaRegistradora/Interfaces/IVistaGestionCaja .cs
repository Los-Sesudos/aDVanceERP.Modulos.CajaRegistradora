using aDVanceERP.Core.Modelos.Comun.Interfaces;
using aDVanceERP.Core.Modelos.Modulos.Caja;
using aDVanceERP.Core.Modelos.Modulos.Inventario;
using aDVanceERP.Core.Vistas.Comun.Interfaces;

namespace aDVanceERP.Modulos.CajaRegistradora.Interfaces {
    internal interface IVistaGestionCaja : IVistaContenedor, IGestorEntidades, IBuscadorEntidades<FiltroBusquedaCajaTurno>, INavegadorTuplasEntidades {
        long IdAlmacenSeleccionado { get; set; }
        string CodigoTurnoActivo { get; }

        event EventHandler? AbrirTurno;
        event EventHandler? CerrarTurno;
        event EventHandler? RegistrarMovimiento;

        /// <summary>Carga el combo de almacenes disponibles.</summary>
        void CargarFiltroAlmacenes(Almacen[] almacenes);

        /// <summary>
        /// Notifica a la vista el turno actualmente abierto para el almacén seleccionado.
        /// null = no hay turno abierto.
        /// </summary>
        void RefrescarEstadoTurnoActivo();
    }
}

using aDVanceERP.Core.Modelos.Modulos.Inventario;
using aDVanceERP.Core.Vistas.Comun.Interfaces;

namespace aDVanceERP.Modulos.CajaRegistradora.Interfaces {
    internal interface IVistaAperturaTurno : IVistaRegistro {
        /// <summary>
        /// Almacén sobre el que se abre el turno. Solo lectura en la 
        /// vista (se inyecta desde el presentador).
        /// </summary>
        Almacen? Almacen { get; set; }

        DateTime FechaApertura { get; set; }

        /// <summary>
        /// Efectivo declarado al abrir la caja.
        /// </summary>
        decimal MontoApertura { get; set; }

        string? Observaciones { get; set; }

        void CargarAlmacenes(Almacen[] almacenes);
    }
}

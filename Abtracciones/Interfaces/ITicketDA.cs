using Abstracciones.Modelos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abtracciones.Interfaces
{
    public interface ITicketDA
    {
        List<Ticket> Obtener();
        Ticket? ObtenerPorId(int id);
        Ticket? ObtenerPorCodigo(string codigo);
        Ticket Agregar(Ticket ticket);
        bool Actualizar(Ticket ticket);

    }
}

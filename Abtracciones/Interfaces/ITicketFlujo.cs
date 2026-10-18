using Abstracciones.Modelos;
using Abtracciones.Modelos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abstracciones.Interfaces
{
    public interface ITicketFlujo
    {
        Resultado<ListaPaginada<Ticket>> Obtener(TicketFiltroDto filtro);
        Resultado<Ticket> ObtenerPorId(int id);
        Resultado<Ticket> ObtenerPorCodigo(string codigo);
        Task<Resultado<Ticket>> Agregar(TicketCreacionDto dto);
        Resultado<Ticket> CambiarEstado(int id, TicketCambioEstadoDto dto);
        Resultado<TicketResumenDto> ObtenerResumen();
        Resultado<Ticket> Reabrir(int id);
        string CalcularETag(Ticket ticket);
    }
}

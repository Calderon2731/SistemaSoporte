using Abtracciones.Modelos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abstracciones.Modelos
{
    // 3. DTO para recibir los filtros por Query String en GET /api/tickets
    public class TicketFiltroDto
    {
        public EstadoTicket? Estado { get; set; }
        public PrioridadTicket? Prioridad { get; set; }
        public int? ClienteId { get; set; }

        public DateTime desde { get; set; }

        public DateTime Hasta { get; set; }

        public string? Q { get; set; }

        public string Orden { get; set; } = "Fecha";

        public int Pagina { get; set; } = 1;

        public int Tamano { get; set; } = 10;

    }
}

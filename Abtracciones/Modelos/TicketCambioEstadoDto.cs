using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Abtracciones.Modelos;

namespace Abstracciones.Modelos
{
    // 2. DTO para PATCH /api/tickets/{id}/estado (Cambiar estado)
    public class TicketCambioEstadoDto
    {
        [Required(ErrorMessage = "El estado es obligatorio")]
        public EstadoTicket? Estado { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "El AgenteId debe ser un número entero mayor o igual a 1.")]
        public int? AgenteId { get; set; }

        public PrioridadTicket? Prioridad { get; set; }

    }
}

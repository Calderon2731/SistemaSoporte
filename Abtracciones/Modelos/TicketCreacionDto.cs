using Abstracciones.Modelos;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abstracciones.Modelos
{
    // 1. DTO para POST /api/tickets (Crear un ticket)
    public class TicketCreacionDto
    {
        [Required(ErrorMessage = "El título es obligatorio")]
        [StringLength(120,MinimumLength =5, ErrorMessage = "El título debe tener entre 5 y 120 caracteres")]
        public string Titulo { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(10, ErrorMessage = "La descripción debe tener al menos 10  caracteres")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "La prioridad es obligatoria")]
        public PrioridadTicket? Prioridad { get; set; }

        [Required(ErrorMessage = "El ID del cliente es obligatorio")]
        [Range(1,int.MaxValue, ErrorMessage = "El ClienteId debe ser un número entero mayor o igual a 1.")]
        public int? ClienteId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El AgenteId debe ser un número entero mayor o igual a 1.")]
        public int AgenteId { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Abstracciones.Modelos
{
    public class TicketResumenDto
    {
        public int Total { get; set; }

        public Dictionary<string, int> PorEstado { get; set; }
        public Dictionary<string, int> PorPrioridad { get; set; }
    }
}

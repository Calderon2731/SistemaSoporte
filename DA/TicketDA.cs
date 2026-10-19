using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Abtracciones.Interfaces;
using Abstracciones.Modelos;

namespace DA
{
    public class TicketDA : ITicketDA
    {
        private static readonly List<Ticket> _tickets = GenerarTicketsIniciales();
        private static int _ultimoId = _tickets.Count;

        private static List<Ticket> GenerarTicketsIniciales()
        { 
            var lista = new List<Ticket>();
            var Prioridades = Enum.GetValues<PrioridadTicket>();
            var fechaBase = new DateTime(2026,1,15,8,0,0, DateTimeKind.Utc);

            for (int i = 1; i <= 25; i++) { 
              var fechaCreacion = fechaBase.AddDays(i);
              var AnioDosDigitos = fechaCreacion.Year % 100;
              // Dejamos solo los primeros 2 en Abierto/EnProceso y los demás en Resuelto/Cerrado
              // para no bloquear de entrada la regla de máximo 3 activos de la Variante 2.

                EstadoTicket estadoInicial = i switch
                {
                    1 => EstadoTicket.Abierto,
                    2 => EstadoTicket.EnProceso,
                    _ => (i % 2 == 0) ? EstadoTicket.Resuelto : EstadoTicket.Cerrado
                };

                lista.Add(new Ticket
                {
                    Id = i,
                    Codigo = $"INC-{i:D6}",
                    Titulo = $"Incidente de soporte tecnico {i}",
                    Descripcion = $"Descripcion del incidente de soporte tecnico {i}",
                    Prioridad = Prioridades[i % Prioridades.Length],
                    Estado = estadoInicial,
                    ClienteId = 1,
                    AgenteId = (i % 2 == 0) ? 10 : null,
                    Reaperturas = 0,
                    FechaCreacion = fechaCreacion,
                    FechaActualizacion = fechaCreacion
                });
            }
            return lista;
        }
        public List<Ticket> Obtener() => _tickets;

        public Ticket? ObtenerPorId(int id) => _tickets.FirstOrDefault(t => t.Id == id);

        public Ticket? ObtenerPorCodigo(string codigo) => _tickets.FirstOrDefault(t => t.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));

        public Ticket Agregar(Ticket ticket)
        {
            ticket.Id = ++_ultimoId;
            _tickets.Add(ticket);
            return ticket;
        }
        public bool Actualizar(Ticket ticket)
        {
            var indice = _tickets.FindIndex(t => t.Id == ticket.Id);
            if (indice == -1)
            {
                return false;
            }
            _tickets[indice] = ticket;
            return true;
        }
    }
}

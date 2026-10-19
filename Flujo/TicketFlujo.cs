using Abstracciones.Modelos;
using Abtracciones.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;


namespace Flujo
{
    public class TicketFlujo : ITicketFlujo
    {
        private readonly ITicketDA _ticketDA;
        private readonly IClienteDA _clienteDA;

        private const int TamanoMaximoVariante = 25;

        public TicketFlujo(ITicketDA ticketDA, IClienteDA clienteDA)
        {
            _ticketDA = ticketDA;
            _clienteDA = clienteDA;
        }

        public async Task<Resultado<Ticket>> Agregar(TicketCreacionDto dto)
        {
            var Cliente = await _clienteDA.ObtenerPorId(dto.ClienteId!.Value);
            if (Cliente is null)
                return Resultado<Ticket>.Fallo(TipoError.NoEncontrado, "Cliente no encontrado", $"No existe un cliente con el Id {dto.ClienteId}.");

            var ahora = DateTime.UtcNow;
            var secuencia = _ticketDA.Obtener().Count + 1;

            var nuevo = new Ticket
            {
                // Variante 1: Formato INC-NNNNNN generado por el sistema
                Codigo = $"INC-{secuencia:D6}",
                Titulo = dto.Titulo.Trim(),
                Descripcion = dto.Descripcion.Trim(),
                Prioridad = dto.Prioridad!.Value,
                Estado = EstadoTicket.Abierto, // Estado inicial siempre Abierto
                ClienteId = dto.ClienteId.Value,
                AgenteId = dto.AgenteId,
                Reaperturas = 0,
                FechaCreacion = ahora,
                FechaActualizacion = ahora
            };

            var creado = _ticketDA.Agregar(nuevo);
            return Resultado<Ticket>.Ok(creado);
        }

        public string CalcularETag(Ticket ticket)
        {
            // Combina los atributos clave del ticket para generar una firma única
            var contenido = $"{ticket.Id}-{ticket.Estado}-{ticket.Prioridad}-{ticket.AgenteId}-{ticket.Reaperturas}-{ticket.FechaActualizacion.Ticks}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(contenido));
            return $"\"{Convert.ToHexString(hash)[..16]}\"";
        }

        public Resultado<Ticket> CambiarEstado(int id, TicketCambioEstadoDto dto)
        {
            var ticket = _ticketDA.ObtenerPorId(id);
            if (ticket is null)
                return Resultado<Ticket>.Fallo(TipoError.NoEncontrado, "Ticket no encontrado", $"No existe un ticket con el Id {id}.");

            var nuevoEstado = dto.Estado!.Value;

            // 1. Validar si la transición entre estados es válida
            if (!EsTransicionValida(ticket.Estado, nuevoEstado))
            {
                return Resultado<Ticket>.Fallo(TipoError.Conflicto, "Transición de estado inválida",
                    $"No se permite cambiar de estado '{ticket.Estado}' a '{nuevoEstado}'.");
            }

            var agenteFinal = dto.AgenteId ?? ticket.AgenteId;
            var prioridadFinal = dto.Prioridad ?? ticket.Prioridad;

            // 2. Regla especial Variante 1 (carnet termina en 6 -> v = 1):
            // No se puede bajar la prioridad a Baja de un ticket que está EnProceso
            if (ticket.Estado == EstadoTicket.EnProceso && prioridadFinal == PrioridadTicket.Baja && ticket.Prioridad != PrioridadTicket.Baja)
            {
                return Resultado<Ticket>.Fallo(TipoError.Conflicto, "Regla de negocio violada",
                    "No se puede bajar la prioridad a Baja de un ticket que se encuentra en estado EnProceso.");
            }

            // 3. Manejo de reapertura si pasa de Resuelto a EnProceso
            if (ticket.Estado == EstadoTicket.Resuelto && nuevoEstado == EstadoTicket.EnProceso)
            {
                ticket.Reaperturas++;
            }

            // 4. Aplicar cambios a la entidad
            ticket.Estado = nuevoEstado;
            ticket.AgenteId = agenteFinal;
            ticket.Prioridad = prioridadFinal;
            ticket.FechaActualizacion = DateTime.UtcNow;

            _ticketDA.Actualizar(ticket);

            // OBLIGATORIO: Esta línea debe estar al final de todo el método
            return Resultado<Ticket>.Ok(ticket);

        } 

        public Resultado<ListaPaginada<Ticket>> Obtener(TicketFiltroDto filtro)
        {
            // Validaciones de parámetros (responde 400 Bad Request si fallan)
            if (filtro.Pagina < 1)
                return Resultado<ListaPaginada<Ticket>>.Fallo(TipoError.Validacion, "Página inválida", "La página debe ser mayor o igual a 1.");

            if (filtro.Tamano < 1 || filtro.Tamano > TamanoMaximoVariante)
                return Resultado<ListaPaginada<Ticket>>.Fallo(TipoError.Validacion, "Tamaño de página inválido", $"El tamaño debe estar entre 1 y {TamanoMaximoVariante}.");

            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde > filtro.Hasta)
                return Resultado<ListaPaginada<Ticket>>.Fallo(TipoError.Validacion, "Rango de fechas inválido", "La fecha 'desde' no puede ser mayor que 'hasta'.");

            var ordenesValidos = new[] { "fecha", "-fecha", "prioridad", "-prioridad" };
            if (!ordenesValidos.Contains(filtro.Orden.ToLowerInvariant()))
                return Resultado<ListaPaginada<Ticket>>.Fallo(TipoError.Validacion, "Orden inválido", "El orden solo acepta: fecha, -fecha, prioridad, -prioridad.");

            IEnumerable<Ticket> query = _ticketDA.Obtener();

            // Filtros opcionales
            if (filtro.Estado.HasValue)
                query = query.Where(t => t.Estado == filtro.Estado.Value);

            if (filtro.Prioridad.HasValue)
                query = query.Where(t => t.Prioridad == filtro.Prioridad.Value);

            if (filtro.ClienteId.HasValue)
                query = query.Where(t => t.ClienteId == filtro.ClienteId.Value);

            if (filtro.Desde.HasValue)
                query = query.Where(t => t.FechaCreacion >= filtro.Desde.Value);

            if (filtro.Hasta.HasValue)
                query = query.Where(t => t.FechaCreacion <= filtro.Hasta.Value);

            // Búsqueda de texto q en Título y Descripción (sin distinguir mayúsculas/minúsculas)
            if (!string.IsNullOrWhiteSpace(filtro.Q))
            {
                var q = filtro.Q.Trim();
                query = query.Where(t =>
                    t.Titulo.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    t.Descripcion.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            // Ordenamiento
            query = filtro.Orden.ToLowerInvariant() switch
            {
                "-fecha" => query.OrderByDescending(t => t.FechaCreacion),
                "prioridad" => query.OrderBy(t => t.Prioridad),
                "-prioridad" => query.OrderByDescending(t => t.Prioridad),
                _ => query.OrderBy(t => t.FechaCreacion)
            };

            var lista = query.ToList();
            var itemsPaginados = lista
                .Skip((filtro.Pagina - 1) * filtro.Tamano)
                .Take(filtro.Tamano)
                .ToList();

            return Resultado<ListaPaginada<Ticket>>.Ok(new ListaPaginada<Ticket>
            {
                Items = itemsPaginados,
                TotalCount = lista.Count,
                Page = filtro.Pagina,
                PageSize = filtro.Tamano
            });
        }

        public Resultado<Ticket> ObtenerPorCodigo(string codigo)
        {
            var ticket = _ticketDA.ObtenerPorCodigo(codigo);
            if (ticket is null)
                return Resultado<Ticket>.Fallo(TipoError.NoEncontrado, "Ticket no encontrado", $"No existe un ticket con el código {codigo}.");

            return Resultado<Ticket>.Ok(ticket);
        }

        public Resultado<Ticket> ObtenerPorId(int id)
        {
            var ticket = _ticketDA.ObtenerPorId(id);
            if (ticket is null)
                return Resultado<Ticket>.Fallo(TipoError.NoEncontrado, "Ticket no encontrado", $"No existe un ticket con el Id {id}.");

            return Resultado<Ticket>.Ok(ticket);
        }

        public Resultado<TicketResumenDto> ObtenerResumen()
        {
            var todos = _ticketDA.Obtener();
            var resumen = new TicketResumenDto
            {
                Total = todos.Count,
                PorEstado = Enum.GetValues<EstadoTicket>().ToDictionary(e => e.ToString(), e => todos.Count(t => t.Estado == e)),
                PorPrioridad = Enum.GetValues<PrioridadTicket>().ToDictionary(p => p.ToString(), p => todos.Count(t => t.Prioridad == p))
            };

            return Resultado<TicketResumenDto>.Ok(resumen);
        }

        public Resultado<Ticket> Reabrir(int id)
        {
            var ticket = _ticketDA.ObtenerPorId(id);
            if (ticket is null)
                return Resultado<Ticket>.Fallo(TipoError.NoEncontrado, "Ticket no encontrado", $"No existe un ticket con el Id {id}.");

            // Solo se puede reabrir si está Resuelto
            if (ticket.Estado != EstadoTicket.Resuelto)
            {
                return Resultado<Ticket>.Fallo(TipoError.Conflicto, "Reapertura no permitida",
                    $"Solo se pueden reabrir tickets en estado Resuelto. El ticket actual está en estado '{ticket.Estado}'.");
            }

            ticket.Estado = EstadoTicket.EnProceso;
            ticket.Reaperturas++;
            ticket.FechaActualizacion = DateTime.UtcNow;

            _ticketDA.Actualizar(ticket);
            return Resultado<Ticket>.Ok(ticket);
        }

        private static bool EsTransicionValida(EstadoTicket actual, EstadoTicket nuevo) =>
            (actual, nuevo) switch
            {
                (EstadoTicket.Abierto, EstadoTicket.EnProceso) => true,
                (EstadoTicket.EnProceso, EstadoTicket.Resuelto) => true,
                (EstadoTicket.Resuelto, EstadoTicket.Cerrado) => true,
                (EstadoTicket.Resuelto, EstadoTicket.EnProceso) => true,
                _ => false
            };
    }
}

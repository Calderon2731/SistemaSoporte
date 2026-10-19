using Abstracciones.Modelos;
using Abtracciones.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/tickets")]
    [Produces("application/json")]
    public class TicketController : Controller
    {
        private readonly ITicketFlujo _ticketFlujo;

        public TicketController(ITicketFlujo ticketFlujo)
        {
            _ticketFlujo = ticketFlujo;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Ticket>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public IActionResult Obtener([FromQuery] TicketFiltroDto filtro)
        {
            var resultado = _ticketFlujo.Obtener(filtro);
            if (!resultado.Exito)
            {
                return MapearError(resultado);
            }
            var paginacion = resultado.Datos!;
            // Headers de paginación obligatorios (Sección 5)
            Response.Headers["X-Total-Count"] = paginacion.TotalCount.ToString();
            Response.Headers["X-Page"] = paginacion.Page.ToString();
            Response.Headers["X-Page-Size"] = paginacion.PageSize.ToString();

            return Ok(paginacion.Items);
        }

        // 2. GET /api/tickets/{id} (Restricción entero >= 1, ETag y soporte de 304)
        [HttpGet("{id:int:min(1)}")]
        [ProducesResponseType(typeof(Ticket), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status304NotModified)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public IActionResult ObtenerPorId(int id)
        {
            var resultado = _ticketFlujo.ObtenerPorId(id);
            if (!resultado.Exito)
            {
                return MapearError(resultado);
            }

            var ticket = resultado.Datos!;
            var etag = _ticketFlujo.CalcularETag(ticket);

            Response.Headers.ETag = etag;

            // Si el cliente envía If-None-Match con el mismo ETag, no se envía el cuerpo
            if (Request.Headers.IfNoneMatch.Contains(etag))
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return Ok(ticket);
        }

        // 3. GET /api/tickets/codigo/{codigo} (Valida formato mediante restricción de ruta)
        [HttpGet("codigo/{codigo:codigoTicket}")]
        [ProducesResponseType(typeof(Ticket), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public IActionResult ObtenerPorCodigo(string codigo)
        {
            var resultado = _ticketFlujo.ObtenerPorCodigo(codigo);
            if (!resultado.Exito)
            {
                return MapearError(resultado);
            }

            return Ok(resultado.Datos);
        }

        // 4. POST /api/tickets (Crear ticket, respuesta 201 con Location header)
        [HttpPost]
        [ProducesResponseType(typeof(Ticket), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Agregar([FromBody] TicketCreacionDto dto)
        {
            var resultado = await _ticketFlujo.Agregar(dto);
            if (!resultado.Exito)
            {
                return MapearError(resultado);
            }

            var creado = resultado.Datos!;

            // Devuelve 201 Created con el header Location apuntando a GET /api/tickets/{id}
            return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
        }

        // 5. PATCH /api/tickets/{id}/estado (Cambio de estado y actualización parcial)
        [HttpPatch("{id:int:min(1)}/estado")]
        [ProducesResponseType(typeof(Ticket), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public IActionResult CambiarEstado(int id, [FromBody] TicketCambioEstadoDto dto)
        {
            var resultado = _ticketFlujo.CambiarEstado(id, dto);
            if (!resultado.Exito)
            {
                return MapearError(resultado);
            }

            return Ok(resultado.Datos);
        }

        // Método de soporte para traducir TipoError a códigos HTTP estándar en formato ProblemDetails
        private IActionResult MapearError<T>(Resultado<T> resultado)
        {
            var statusCode = resultado.Error switch
            {
                TipoError.Validacion => StatusCodes.Status400BadRequest,
                TipoError.NoEncontrado => StatusCodes.Status404NotFound,
                TipoError.Conflicto => StatusCodes.Status409Conflict,
                TipoError.ClienteNoExiste => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status500InternalServerError
            };

            return Problem(
                title: resultado.Titulo,
                detail: resultado.Detalle,
                statusCode: statusCode
            );


        }
    }
}

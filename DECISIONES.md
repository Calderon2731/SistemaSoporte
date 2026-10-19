\# Bitácora de Decisiones Técnicas - Práctica Programada 1

\*\*Estudiante:\*\* Kendall  

\*\*Curso:\*\* SC-701 Programación Avanzada en Web  

\*\*Variante Personal:\*\* 1 ($v = 1$)  



\---



\## 1. Su variante (v) y nombre completo

\* \*\*Nombre completo:\*\* Kendall

\* \*\*Último dígito del carnet estudiantil:\*\* 6

\* \*\*Cálculo de la variante:\*\* $v = 6 \\pmod 5 = 1$

\* \*\*Parámetros asignados a la Variante 1:\*\*

&#x20; \* Tamaño máximo de página: 25.

&#x20; \* Formato del código de ticket: `INC-NNNNNN` (ejemplo: `INC-000001`).

&#x20; \* Regla de negocio especial: No se puede bajar la prioridad a "Baja" de un ticket que se encuentra en estado "EnProceso" (responde `409 Conflict` ante intentos de degradación).



\---



\## 2. El ClienteId enviado al crear un ticket no existe: ¿responde 400, 404 o 422? Justifique frente a las alternativas

Se responde con \*\*`422 Unprocessable Content`\*\* (o alternativamente `400 Bad Request` si se categoriza como error semántico del payload).



\* \*\*Por qué NO 404 (Not Found):\*\* En la semántica REST/HTTP (RFC 9110), el código 404 indica que el recurso identificado por el URI de la petición (`/api/tickets`) no existe. Si devolvemos 404 porque falló una clave foránea interna, el cliente asumirá erróneamente que la ruta del endpoint no existe o está mal escrita en el servidor.

\* \*\*Por qué NO 400 (Bad Request):\*\* El código 400 se reserva principalmente para errores de sintaxis, formato mal estructurado (un JSON roto), parámetros faltantes requeridos o tipos de datos incompatibles (enviar un string donde va un entero). En este caso, el JSON es sintácticamente impecable y el `ClienteId` es un número entero válido.

\* \*\*Por qué SÍ 422 (Unprocessable Content):\*\* Según RFC 9110 / RFC 4918, el código 422 significa que el servidor entendió el tipo de contenido y la sintaxis es correcta, pero no puede procesar las instrucciones contenidas debido a un error semántico o violación de integridad referencial del dominio (el cliente referenciado no existe en el sistema de almacenamiento).



\---



\## 3. Un id como abc en /api/tickets/abc: ¿por qué la restricción de ruta responde 404 mientras que un body inválido responde 400? ¿Cuándo usaría cada mecanismo?

\* \*\*¿Por qué la restricción de ruta responde 404?:\*\* La restricción de ruta (`{id:int:min(1)}` o `IRouteConstraint`) opera dentro del motor de Routing de ASP.NET Core \*\*antes\*\* de seleccionar o ejecutar cualquier acción del controlador. Si el valor en la URL (`abc`) no cumple la restricción, el enrutador considera que esa URL no coincide con ningún endpoint registrado en la tabla de rutas del servidor, devolviendo directamente `404 Not Found` (Ruta no encontrada).

\* \*\*¿Por qué un body inválido responde 400?:\*\* Para procesar el body, la ruta ya fue encontrada con éxito (hubo coincidencia de endpoint). El error ocurre en una etapa posterior: durante el \*Model Binding\* o la validación del modelo (`DataAnnotations` ejecutadas automáticamente por `\[ApiController]`). Al ser un fallo en los datos o tipos del mensaje enviado por el cliente, corresponde `400 Bad Request`.

\* \*\*¿Cuándo usar cada mecanismo?:\*\*

&#x20; \* \*Restricción de ruta:\* Se usa para identificar el recurso en el URI. Si el valor no cumple el formato esperado, no representa un recurso válido del sistema y no debe gastar ciclos de CPU ejecutando el controlador.

&#x20; \* \*Validación en body (400):\* Se usa para los datos de entrada que el recurso necesita para operar (cuerpo de la petición), donde se requiere inspeccionar los campos y devolver detalles específicos al usuario mediante `ProblemDetails`.



\---



\## 4. ¿Cómo comunica Flujo un error de regla de negocio al API sin depender de HTTP? Compare al menos dos alternativas, diga cuál eligió y qué costo tiene

La capa `Flujo` no debe referenciar paquetes web (`Microsoft.AspNetCore.\*`) para mantener la arquitectura limpia y desacoplada del protocolo de transporte.



\* \*\*Alternativa A: Excepciones personalizadas (Custom Exceptions):\*\*

&#x20; \* \*Cómo funciona:\* La capa Flujo lanza excepciones propias de dominio (ej. `ReglaNegocioException`, `EntidadNoEncontradaException`) y un Middleware en la capa API las atrapa y las transforma en respuestas HTTP.

&#x20; \* \*Costo:\* En .NET, instanciar y lanzar excepciones tiene un costo computacional elevado debido a la captura del \*stack trace\* en memoria. Además, usar excepciones para controlar el flujo predecible de la lógica de negocio se considera un anti-patrón de diseño.

\* \*\*Alternativa B: Patrón Resultado (Result Pattern - Elección tomada):\*\*

&#x20; \* \*Cómo funciona:\* Los métodos de Flujo devuelven una clase genérica `Resultado<T>` que encapsula el objeto de retorno junto con un enum neutro (`TipoError.Ninguno`, `TipoError.Validacion`, `TipoError.NoEncontrado`, `TipoError.Conflicto`, `TipoError.ClienteNoExiste`) y mensajes descriptivos (`Titulo`, `Detalle`).

&#x20; \* \*Costo y balance:\* No genera penalización de rendimiento (\*zero-overhead\*), el flujo es explícito y el código es fuertemente tipado en tiempo de compilación. Su único costo es que el controlador debe evaluar `if (!resultado.Exito)` para mapear el enum neutro al código HTTP respectivo.



\---



\## 5. Paginación: ¿por qué limitar el tamaño máximo de página? ¿Qué devuelve una página fuera de rango y por qué? ¿Qué es la paginación por cursor y por qué no se usó aquí?

\* \*\*¿Por qué limitar el tamaño máximo (25 en variante 1)?:\*\* Evita ataques de denegación de servicio (DoS) o agotamiento de memoria RAM en el servidor si un cliente solicita millones de registros en una sola consulta (`tamano=1000000`).

\* \*\*¿Qué devuelve una página fuera de rango y por qué?:\*\* Devuelve una lista vacía `\[]` con código `200 OK` (y los headers `X-Total-Count`, `X-Page`, `X-Page-Size` informando la realidad de la colección). Es 200 y no 404 porque la consulta y el recurso existen; simplemente la ventana solicitada no contiene elementos en ese momento, lo que evita romper clientes frontend que consumen arreglos.

\* \*\*Paginación por cursor y por qué no se usó:\*\* La paginación por cursor utiliza un apuntador o identificador del último elemento visto (por ejemplo: `?cursor=INC-000025`) para traer los siguientes $N$ registros usando una condición `WHERE id > cursor` en lugar de `OFFSET`. No se utilizó aquí porque los requerimientos de la práctica exigen navegación bidireccional (ir a página anterior/siguiente de forma arbitraria) y ordenamiento dinámico por múltiples campos (`fecha`, `prioridad`), escenarios donde la paginación por desplazamiento (\*offset/skip-take\*) es más directa de implementar.



\---



\## 6. ETag: ¿qué problema resuelve? ¿Fuerte o débil, y cómo lo calculó? ¿Qué situación no cubre?

\* \*\*Problema que resuelve:\*\* Optimiza el uso de la red y la latencia del cliente implementando caché condicional. Evita transferir el cuerpo del ticket cuando este no ha sufrido modificaciones en el servidor desde la última lectura del cliente.

\* \*\*¿Fuerte o débil, y cómo se calculó?:\*\* Se implementó un \*\*ETag Fuerte\*\*. Se calculó aplicando un algoritmo de hash criptográfico SHA-256 sobre una cadena compuesta por el identificador y todos los campos mutables del ticket: `"{Id}-{Estado}-{Prioridad}-{AgenteId}-{Reaperturas}-{FechaActualizacion.Ticks}"`. Esto garantiza que cualquier cambio a nivel de bytes en la entidad genere un hash completamente diferente.

\* \*\*¿Qué situación no cubre?:\*\* No resuelve por sí solo las colisiones de concurrencia en modificaciones concurrentes (\*Lost Update Problem\*) a menos que se exija el encabezado `If-Match` en las peticiones `PUT`/`PATCH` respondiendo `412 Precondition Failed` cuando dos usuarios intentan modificar el mismo ticket simultáneamente.



\---



\## 7. Una transición de estado inválida: ¿por qué 409 y no 400 ni 422?

El código \*\*`409 Conflict`\*\* está especificado por el estándar HTTP (RFC 9110) para situaciones donde la petición no puede completarse debido a un conflicto con el estado actual del recurso en el servidor.

\* No es 400 porque la sintaxis del JSON y los valores enviados son válidos.

\* No es 422 porque el valor del estado en sí mismo es un estado válido del dominio. El impedimento no es el dato enviado, sino que el ticket se encuentra en un estado que no permite esa transición según la máquina de estados del ciclo de vida (por ejemplo, intentar pasar un ticket de `Cerrado` a `Abierto`).



\---



\## 8. Fuentes consultadas

\* \[RFC 9457 - Problem Details for HTTP APIs](https://datatracker.ietf.org/doc/html/rfc9457)

\* \[RFC 9110 - HTTP Semantics: Status Codes, ETag, Preconditions](https://datatracker.ietf.org/doc/html/rfc9110)

\* \[Microsoft Learn: Enrutamiento en ASP.NET Core e IRouteConstraint](https://learn.microsoft.com/es-es/aspnet/core/fundamentals/routing)

\* \[Microsoft Learn: Manejo de errores en APIs web de ASP.NET Core](https://learn.microsoft.com/es-es/aspnet/core/web-api/handle-errors)

\* \[Microsoft Learn: Serialización de enumeraciones con JsonStringEnumConverter](https://learn.microsoft.com/es-es/dotnet/standard/serialization/system-text-json/customize-properties?pivots=dotnet-8-0#enums-as-strings)



\---



\## 9. Declaración de uso de IA

\* \*\*Asistente utilizado:\*\* Gemini.

\* \*\*Qué se le pidió:\*\*

&#x20; 1. Orientación sobre la arquitectura de 4 capas para estructurar el patrón `Resultado<T>` y desacoplar `Flujo` de HTTP.

&#x20; 2. Implementación de una restricción de ruta personalizada (`IRouteConstraint`) con expresiones regulares para la validación del código `INC-NNNNNN`.

&#x20; 3. Cálculo de ETag con SHA-256 y soporte del encabezado `If-None-Match` para responder `304 Not Modified`.

&#x20; 4. Depuración del error de enlace en el POST con tipos anulables (`int?`) en `AgenteId` y configuración de `ProblemDetails`.

\* \*\*Qué se corrigió o rechazó:\*\*

&#x20; 1. Se corrigió la variante inicial para adaptarla estrictamente a la Variante 1 ($v=1$), cambiando el formato de código a `INC-NNNNNN`, el tamaño máximo de página a 25 y la regla especial de prioridad Baja en estado EnProceso.

&#x20; 2. Se rechazó el uso de `Microsoft.AspNetCore.JsonPatch` en `PATCH /api/tickets/{id}/estado`, utilizando en su lugar un DTO directo con validaciones de negocio en `Flujo` para mantener el código más simple y desacoplado.


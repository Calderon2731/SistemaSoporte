# Sistema Soporte - API de Tickets y Clientes
**Curso:** SC-701 Programación Avanzada en Web  
**Evaluación:** Práctica Programada 1  
**Estudiante:** Kendall  
**Variante:** 1 ($v = 1$, carnet terminado en 6)  

---

## 1. Descripción del Proyecto

API RESTful desarrollada en **.NET 8 / .NET 9** bajo una arquitectura limpia en 4 capas (`Abtracciones`, `DA`, `Flujo` y `API`), orientada a la gestión de incidentes y mesa de ayuda técnica:

* **Módulo de Clientes:** Persistencia en SQL Server LocalDB mediante Entity Framework Core.
* **Módulo de Tickets:** Persistencia en memoria (`TicketDA`) con 25 tickets generados al arrancar, cumpliendo con la **Variante 1**:
  * Formato de código: `INC-NNNNNN` validado mediante `IRouteConstraint`.
  * Límite máximo de página: 25 elementos.
  * Regla de negocio especial: No se permite degradar la prioridad a "Baja" si el ticket se encuentra en estado "EnProceso" (responde `409 Conflict`).
* **Estándares HTTP y Resiliencia:** Respuestas de error estandarizadas con `ProblemDetails` (RFC 9457), caché condicional con `ETag` / `If-None-Match` (304 Not Modified), paginación con headers (`X-Total-Count`, `X-Page`, `X-Page-Size`) y endpoints Minimal API.
* **Frontend Estático:** Interfaz web ligera en `wwwroot/tickets.html` que consume el API mediante Fetch API.

---

## 2. Requisitos Previos

* [.NET SDK 8.0 o 9.0](https://dotnet.microsoft.com/download)
* [SQL Server Express LocalDB](https://learn.microsoft.com/es-es/sql/database-engine/configure-windows/sql-server-express-localdb) (instancia por defecto `(localdb)\MSSQLLocalDB`)
* Herramienta de línea de comandos `dotnet ef` (opcional si se usan migraciones por CLI)

---

## 3. Configuración y Cadena de Conexión

Verifique que en el archivo `API/appsettings.json` la cadena de conexión apunte a su instancia local de SQL Server:

```json
{
  "ConnectionStrings": {
    "SistemaSoporte": "Server=(localdb)\\MSSQLLocalDB;Database=SistemaSoporteDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
!


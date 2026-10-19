using Abstracciones.Modelos;
using Abtracciones.Interfaces;
using API.Constraints;
using DA;
using DA.Contexto;
using Flujo;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);


//activar ProblemsDetails conforme a RFC 9457
builder.Services.AddProblemDetails();

// Add services to the container

// 2. Controladores y conversión de Enums a texto JSON

builder.Services.AddControllers().AddJsonOptions(opciones =>
{
    opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});


// Soporte de Enums como texto en Minimal APIs
builder.Services.ConfigureHttpJsonOptions(opciones =>
{
    opciones.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// 3. Registrar restricción de ruta personalizada
builder.Services.Configure<RouteOptions>(opciones =>
{
    opciones.ConstraintMap.Add("codigoTicket", typeof(CodigoTicketContraint));
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//Inyeccion de dependencias
builder.Services.AddScoped<IClienteDA, ClienteDA>();
builder.Services.AddScoped<IClienteFlujo, ClienteFlujo>();
builder.Services.AddScoped<ITicketDA, TicketDA>();
builder.Services.AddScoped<ITicketFlujo, TicketFlujo>();

var cadenaConexion = builder.Configuration.GetConnectionString("SistemaSoporte") ??
    throw new InvalidOperationException("Falta la cadena de conexion 'Sistema Soporte' en la configuraion");

builder.Services.AddDbContext<SistemaSoporteContexto>(opciones => opciones.UseSqlServer(cadenaConexion));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// -------------------------------------------------------------
// ENDPOINTS MINIMAL API: CLIENTES (con async/await corregido)
// -------------------------------------------------------------
app.MapGet("/min/clientes", (IClienteFlujo flujo) => Results.Ok(flujo.Obtener()));

app.MapGet("/min/clientes/{id:int}", (int id, IClienteFlujo flujo) =>
{
    var cliente = flujo.ObtenerPorId(id);
    return cliente is null ? Results.NotFound() : Results.Ok(cliente);
});

app.MapPost("/min/clientes", (Cliente cliente, IClienteFlujo flujo) =>
{
    var creado = flujo.Agregar(cliente);
    return Results.Created($"/min/clientes/{creado.Id}",creado);
});

// -------------------------------------------------------------
// ENDPOINTS MINIMAL API: TICKETS (Sección 5 de la práctica)
// -------------------------------------------------------------

// GET /min/tickets/resumen
app.MapGet("/min/tickets/resumen", (ITicketFlujo flujo) =>
{
    var resultado = flujo.ObtenerResumen();
    return Results.Ok(resultado);
});

app.MapPost("/min/tickets/{id:int:min(1)}/reabrir", (int id, ITicketFlujo flujo) =>
{
    var resultado = flujo.Reabrir(id);
    if (!resultado.Exito)
    {
        var codigoEstado = resultado.Error == TipoError.NoEncontrado ? StatusCodes.Status404NotFound : StatusCodes.Status409Conflict;

        return Results.Problem
        (
            title: resultado.Titulo,
            detail: resultado.Detalle,
            statusCode: codigoEstado
        );
    }
    return Results.Ok(resultado.Datos);
});


// -------------------------------------------------------------
// SERVIR ARCHIVOS ESTÁTICOS Y ENRUTAR CONTROLADORES
// -------------------------------------------------------------
//nos permite que "/" sirva wwwroot/index,html sin escribir el nombre del archivo
app.UseDefaultFiles();

//Habilita brindar(servir) al servidor los archivos que viven en wwwroot (WebRoot)
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();

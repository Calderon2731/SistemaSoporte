using System;
using System.Collections.Generic;
using System.Text;

namespace Abstracciones.Modelos
{
    public enum TipoError
    {
        Ninguno, 
        Validacion,// Cuando un filtro o dato de entrada es inválido
        NoEncontrado,// Cuando el Ticket buscado no existe
        Conflicto,// Cuando falla una transición de estado o la regla de tu variante
        ClienteNoExiste,// Cuando el ClienteId enviado al crear el ticket no existe en ClienteDA

    }
    public class Resultado<T>
    {
        public bool Exito => Error == TipoError.Ninguno;
        public T? Datos { get; set; }
        public TipoError Error { get; set; } = TipoError.Ninguno;
        public string Titulo { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;

        // Método rápido para devolver cuando todo salió bien
        public static Resultado<T> Ok(T datos) => new()
        {
            Datos = datos,
            Error = TipoError.Ninguno,
        };


        public static Resultado<T> Fallo(TipoError error, string titulo, string detalle) => new()
        {
            Error = error,
            Titulo = titulo,
            Detalle = detalle
        };
    }
}

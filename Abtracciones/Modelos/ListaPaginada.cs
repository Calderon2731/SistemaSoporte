using System;
using System.Collections.Generic;
using System.Text;

namespace Abstracciones.Modelos
{
    // 4. DTO de salida para transportar los items de la página junto con los totales
    public class ListaPaginada<T>
    {
        public List<T> Items { get; set; }

        public int TotalCount { get; set; }
        public int Page { get; set; }

        public int PageSize { get; set; }

    }
}

namespace API.Constraints
{
    using System.Text.RegularExpressions;
    using Microsoft.AspNetCore.Routing;
    public class CodigoTicketContraint : IRouteConstraint
    {
        private static readonly Regex RegexCodigo = new(@"^INC-\d{6}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (!values.TryGetValue(routeKey, out var valor) || valor is null) 
            {
                return false;
            }
            var codigo = Convert.ToString(valor);

            return !string.IsNullOrEmpty(codigo) && RegexCodigo.IsMatch(codigo);
        }
    }
}

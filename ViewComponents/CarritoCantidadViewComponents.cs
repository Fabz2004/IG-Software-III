using ALODAN.Helpers;
using ALODAN.Models;
using Microsoft.AspNetCore.Mvc;

namespace ALODAN.ViewComponents
{
    public class CarritoCantidadViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            // Recuperar el carrito desde la sesión
            var carrito = HttpContext.Session.GetObject<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();

            // Calcular la cantidad total de productos
            int cantidadTotal = carrito.Sum(c => c.Cantidad);

            return View(cantidadTotal);
        }
    }
}

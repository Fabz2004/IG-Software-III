using ALODAN.Datos;
using ALODAN.Helpers;
using ALODAN.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace ALODAN.Controllers
{
    public class CarritoController : Controller
    {
        private const string CarritoSessionKey = "Carrito";
        private readonly ApplicationDbContext _context;

        public CarritoController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var carrito = HttpContext.Session.GetObject<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();

            // 🧮 Calcular subtotal
            decimal subtotal = carrito.Sum(c => c.Subtotal);

            // 🧮 Calcular descuento según monto
            decimal descuento = 0;
            if (subtotal >= 300)
                descuento = subtotal * 0.15m;
            else if (subtotal >= 200)
                descuento = subtotal * 0.10m;
            else if (subtotal >= 100)
                descuento = subtotal * 0.05m;

            // 🧾 Calcular total final
            decimal totalFinal = subtotal - descuento;

            // 📦 Pasar valores a la vista
            ViewBag.Subtotal = subtotal;
            ViewBag.Descuento = descuento;
            ViewBag.Total = totalFinal;
            ViewBag.CantidadCarrito = carrito.Count;

            return View(carrito);
        }


        public IActionResult Agregar(int id, string talla, string color)
        {
            var producto = _context.Productos.Find(id);
            if (producto == null) return NotFound();

            var carrito = HttpContext.Session.GetObject<List<CarritoItem>>(CarritoSessionKey) ?? new List<CarritoItem>();

            var item = carrito.FirstOrDefault(c => c.ProductoId == id && c.Talla == talla && c.Color == color);
            if (item != null)
                item.Cantidad++;
            else
                carrito.Add(new CarritoItem
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    ImagenUrl = producto.ImagenUrl,
                    Precio = producto.Precio,
                    Cantidad = 1,
                    Talla = talla,
                    Color = color
                });

            HttpContext.Session.SetObject(CarritoSessionKey, carrito);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Eliminar(int id)
        {
            var carrito = HttpContext.Session.GetObject<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
            var item = carrito.FirstOrDefault(c => c.ProductoId == id);

            if (item != null)
                carrito.Remove(item);

            HttpContext.Session.SetObject("Carrito", carrito);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ProcederPago()
        {
            var usuarioJson = HttpContext.Session.GetString("UsuarioLogueado");

            if (usuarioJson == null)
            {
                HttpContext.Session.SetString("ReturnUrl", Url.Action("Index", "Checkout"));
                ViewBag.MostrarLogin = true;

                var carrito = HttpContext.Session.GetObject<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
                ViewBag.Total = carrito.Sum(c => c.Subtotal);
                return View("Index", carrito);
            }

            return RedirectToAction("Index", "Checkout");
        }

        [HttpPost]
        public IActionResult ActualizarCantidad(int productoId, int nuevaCantidad)
        {
            var carrito = HttpContext.Session.GetObject<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
            var item = carrito.FirstOrDefault(c => c.ProductoId == productoId);

            if (item != null && nuevaCantidad > 0)
                item.Cantidad = nuevaCantidad;

            HttpContext.Session.SetObject("Carrito", carrito);
            return RedirectToAction("Index");
        }
    }
}

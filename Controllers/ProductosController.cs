/*
using ALODAN.Datos;
using ALODAN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ALODAN.Controllers
{
    public class ProductosController : Controller
    {
        private readonly ILogger<ProductosController> _logger;
        private readonly ApplicationDbContext _context;

        public ProductosController(ILogger<ProductosController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Página principal o catálogo destacado
        public IActionResult Inicio()
        {
            return View();
        }

        // Listar productos por categoría o todos
        public IActionResult Index(string categoria)
        {
            var productos = string.IsNullOrEmpty(categoria)
                ? _context.Productos.ToList()
                : _context.Productos.Where(p => p.Categoria == categoria).ToList();

            ViewBag.Categoria = categoria;
            return View(productos);
        }

        // Mostrar los detalles de un producto
        public IActionResult Detalles(int id)
        {
            var producto = _context.Productos.FirstOrDefault(p => p.Id == id);
            if (producto == null)
                return NotFound();

            // Si guardas las características o colores como texto separado por comas,
            // puedes dividirlos aquí para mostrarlos más fácil en la vista.
            ViewBag.Caracteristicas = producto.Caracteristicas?.Split(',').Select(c => c.Trim()).ToList() ?? new List<string>();
            ViewBag.Tallas = producto.Talla?.Split(',').Select(t => t.Trim()).ToList() ?? new List<string>();
            ViewBag.Colores = producto.Colores?.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrWhiteSpace(c)).ToList() ?? new List<string>();

            return View(producto);
        }
    }
}
*/

using ALODAN.Datos;
using ALODAN.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace ALODAN.Controllers
{
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 Página de categoría (mantienes igual)

        public IActionResult Inicio()
        {
            return View();
        }
        public IActionResult Index(string categoria)
        {
            var productos = string.IsNullOrEmpty(categoria)
                ? _context.Productos.ToList()
                : _context.Productos.Where(p => p.Categoria == categoria).ToList();

            ViewBag.Categoria = categoria;
            return View(productos);
        }

        // 🔹 Acción para buscar con filtros
        [HttpGet]
   
        public IActionResult Buscar(string busqueda, string talla, decimal? precioMin, decimal? precioMax)
        {
            var productos = _context.Productos.AsQueryable();

            if (!string.IsNullOrEmpty(busqueda))
            {
                var texto = busqueda.ToLower().Trim();
                productos = productos.Where(p =>
                    p.Nombre.ToLower().Contains(texto) ||
                    p.Categoria.ToLower().Contains(texto) ||
                    p.Descripcion.ToLower().Contains(texto));
            }

            if (!string.IsNullOrEmpty(talla))
                productos = productos.Where(p => p.Talla.ToLower().Contains(talla.ToLower()));

            if (precioMin.HasValue)
                productos = productos.Where(p => p.Precio >= precioMin.Value);

            if (precioMax.HasValue)
                productos = productos.Where(p => p.Precio <= precioMax.Value);

            var lista = productos.ToList();
            if (!lista.Any())
                ViewBag.SinResultados = true;

            return View("ResultadosBusqueda", lista);
        }


        // 🔹 Detalles (sin cambios)
        public IActionResult Detalles(int id)
        {
            var producto = _context.Productos.FirstOrDefault(p => p.Id == id);
            if (producto == null) return NotFound();

            return View(producto);
        }
    }
}

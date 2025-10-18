using ALODAN.Datos;
using ALODAN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ALODAN.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔒 Verifica si el usuario actual tiene rol de administrador
        private bool EsAdmin()
        {
            var rol = HttpContext.Session.GetString("Rol");
            return rol == "Admin";
        }

        // 🔹 PANEL PRINCIPAL DEL ADMIN
        public IActionResult Dashboard()
        {
            if (!EsAdmin()) return RedirectToAction("Inicio", "Productos");

            ViewBag.TotalProductos = _context.Productos.Count();
            ViewBag.TotalPedidos = _context.Pedidos.Count();
            ViewBag.PedidosPendientes = _context.Pedidos.Where(p => p.Estado != "Entregado").Count();

            return View();
        }

        // ========== 🔹 PRODUCTOS ==========
        public IActionResult Productos()
        {
            if (!EsAdmin()) return RedirectToAction("Inicio", "Productos");

            var productos = _context.Productos.ToList();
            return View(productos);
        }

        [HttpGet]
        public IActionResult CrearProducto()
        {
            if (!EsAdmin()) return RedirectToAction("Inicio", "Productos");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearProducto(Producto producto)
        {
            if (!EsAdmin()) return RedirectToAction("Inicio", "Productos");

            if (ModelState.IsValid)
            {
                _context.Productos.Add(producto);
                _context.SaveChanges();
                return RedirectToAction("Productos");
            }
            return View(producto);
        }

        [HttpGet]
        public IActionResult EditarProducto(int id)
        {
            if (!EsAdmin()) return RedirectToAction("Inicio", "Productos");

            var producto = _context.Productos.Find(id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarProducto(Producto producto)
        {
            if (!EsAdmin()) return RedirectToAction("Inicio", "Productos");

            if (ModelState.IsValid)
            {
                _context.Productos.Update(producto);
                _context.SaveChanges();
                return RedirectToAction("Productos");
            }
            return View(producto);
        }

        [HttpPost]
        public IActionResult EliminarProducto(int id)
        {
            if (!EsAdmin()) return RedirectToAction("Inicio", "Productos");

            var producto = _context.Productos.Find(id);
            if (producto == null) return NotFound();

            _context.Productos.Remove(producto);
            _context.SaveChanges();
            return RedirectToAction("Productos");
        }

        // ========== 🔹 PEDIDOS ==========
        public IActionResult Pedidos()
        {
            if (!EsAdmin()) return RedirectToAction("Inicio", "Productos");

            var pedidos = _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .OrderByDescending(p => p.FechaPedido)
                .ToList();

            return View(pedidos);
        }

        [HttpPost]
        public IActionResult ActualizarEstado(int id, string nuevoEstado)
        {
            if (!EsAdmin()) return RedirectToAction("Inicio", "Productos");

            var pedido = _context.Pedidos.Find(id);
            if (pedido == null) return NotFound();

            pedido.Estado = nuevoEstado;
            _context.SaveChanges();

            return RedirectToAction("Pedidos");
        }
    }
}

using ALODAN.Datos;
using ALODAN.Helpers;
using ALODAN.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text.Json;

namespace Alodan.Controllers
{
    public class CuentaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string SessionUsuario = "UsuarioLogueado";

        public CuentaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 LOGIN
        [HttpPost]
        public IActionResult Login(string Email, string Password)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == Email && u.Password == Password);

            if (usuario == null)
            {
                ViewBag.ErrorLogin = "Correo o contraseña incorrectos.";
                return View("Login");
            }

            // Guardar usuario en sesión
            HttpContext.Session.SetString("UsuarioLogueado", JsonSerializer.Serialize(usuario));

            // ✅ Redirigir al checkout si venía del carrito
            var returnUrl = HttpContext.Session.GetString("ReturnUrl");
            if (!string.IsNullOrEmpty(returnUrl))
            {
                HttpContext.Session.Remove("ReturnUrl");
                return Redirect(returnUrl);
            }

            // Si no hay ReturnUrl, ir al perfil
            return RedirectToAction("Perfil");
        }

        // 🔹 REGISTRO
        [HttpPost]
        public IActionResult Registro(Usuario nuevoUsuario)
        {
            // ✅ Validación defensiva para evitar NullReference
            if (nuevoUsuario == null)
            {
                TempData["ErrorRegistro"] = "Error al procesar el registro. Intenta nuevamente.";
                return RedirectToAction("Index", "Carrito");
            }

            // 🔹 Validar contraseña mínima 8 caracteres
            if (string.IsNullOrWhiteSpace(nuevoUsuario.Password) || nuevoUsuario.Password.Length < 8)
            {
                TempData["ErrorRegistro"] = "La contraseña debe tener al menos 8 caracteres.";
                return RedirectToAction("Index", "Carrito");
            }

            // 🔹 Validar teléfono (9 dígitos numéricos)
            if (string.IsNullOrWhiteSpace(nuevoUsuario.Telefono) || nuevoUsuario.Telefono.Length != 9 || !nuevoUsuario.Telefono.All(char.IsDigit))
            {
                TempData["ErrorRegistro"] = "El número de teléfono debe tener exactamente 9 dígitos.";
                return RedirectToAction("Index", "Carrito");
            }

            // 🔹 Validar correo duplicado
            if (_context.Usuarios.Any(u => u.Email == nuevoUsuario.Email))
            {
                TempData["ErrorRegistro"] = "Este correo ya está registrado. Por favor, utiliza otro.";
                return RedirectToAction("Index", "Carrito");
            }

            // ✅ Guardar usuario en BD
            _context.Usuarios.Add(nuevoUsuario);
            _context.SaveChanges();

            // ✅ Iniciar sesión automáticamente
            HttpContext.Session.SetString("UsuarioLogueado", JsonSerializer.Serialize(nuevoUsuario));

            // ✅ Mostrar mensaje de éxito
            TempData["RegistroExitoso"] = true;

            // 🔹 Si el carrito tiene productos → ir a checkout
            var carrito = HttpContext.Session.GetObject<List<CarritoItem>>("Carrito");
            if (carrito != null && carrito.Any())
                return RedirectToAction("Index", "Checkout");

            // 🔹 Si no hay productos → ir al inicio
            return RedirectToAction("Inicio", "Productos");
        }

        // 🔹 Verificar correo duplicado (AJAX)
        [HttpGet]
        public JsonResult VerificarCorreo(string email)
        {
            bool existe = _context.Usuarios.Any(u => u.Email == email);
            return Json(existe);
        }

        // 🔹 PERFIL
        public IActionResult Perfil()
        {
            var usuarioJson = HttpContext.Session.GetString(SessionUsuario);

            if (usuarioJson == null)
                return View("SinSesion");

            var usuario = JsonSerializer.Deserialize<Usuario>(usuarioJson);
            return View(usuario);
        }

        // 🔹 LOGOUT
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Inicio", "Productos");
        }
    }
}

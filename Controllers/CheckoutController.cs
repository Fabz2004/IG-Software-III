using Microsoft.AspNetCore.Mvc;
using ALODAN.Models;
using System.Text.Json;

namespace ALODAN.Controllers
{
    public class CheckoutController : Controller
    {
        private const string SessionUsuario = "UsuarioLogueado";

        public IActionResult Index()
        {
            var usuarioJson = HttpContext.Session.GetString(SessionUsuario);
            if (usuarioJson == null)
                return RedirectToAction("Index", "Carrito");

            var usuario = JsonSerializer.Deserialize<Usuario>(usuarioJson);
            return View(usuario);
        }
    }
}

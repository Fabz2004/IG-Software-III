using ALODAN.Datos;
using ALODAN.Helpers;
using ALODAN.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;



namespace ALODAN.Controllers
{
    public class ComprasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public ComprasController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // 🔹 Vista de compras del usuario logueado
        public IActionResult Index()
        {
            var usuarioJson = HttpContext.Session.GetString("UsuarioLogueado");

            if (usuarioJson == null)
            {
                ViewBag.UsuarioNoLogueado = true;
                return View(new List<Pedido>());
            }

            var usuario = JsonSerializer.Deserialize<Usuario>(usuarioJson);

            var pedidos = _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .Where(p => p.UsuarioId == usuario.Id)
                .OrderByDescending(p => p.FechaPedido)
                .ToList();

            return View(pedidos);
        }

        // 🔹 Confirmar compra → guarda pedido y envía correo con constancia PDF
        [HttpPost]
        public async Task<IActionResult> ConfirmarCompra()
        {
            var carrito = HttpContext.Session.GetObject<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
            if (!carrito.Any())
                return RedirectToAction("Index", "Carrito");

            var usuarioJson = HttpContext.Session.GetString("UsuarioLogueado");
            if (usuarioJson == null)
                return RedirectToAction("Perfil", "Cuenta");

            var usuario = JsonSerializer.Deserialize<Usuario>(usuarioJson);

            // Crear pedido
            // Obtener el último número de pedido del usuario
            var ultimoNumero = _context.Pedidos
                .Where(p => p.UsuarioId == usuario.Id)
                .OrderByDescending(p => p.NumeroPedido)
                .Select(p => p.NumeroPedido)
                .FirstOrDefault();

            var pedido = new Pedido
            {
                UsuarioId = usuario.Id,
                FechaPedido = DateTime.Now,
                Estado = "Solicitud recibida",
                Total = carrito.Sum(c => c.Subtotal),
                NumeroPedido = ultimoNumero + 1, // ← consecutivo por usuario
                Detalles = carrito.Select(c => new PedidoDetalle
                {
                    ProductoId = c.ProductoId,
                    Cantidad = c.Cantidad,
                    PrecioUnitario = c.Precio,
                    Talla = c.Talla,
                    Color = c.Color
                }).ToList()
            };


            _context.Pedidos.Add(pedido);
            _context.SaveChanges();

            // 🔹 Volver a cargar el pedido con los productos incluidos
            pedido = _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefault(p => p.Id == pedido.Id);

            // Eliminar carrito
            HttpContext.Session.Remove("Carrito");

            // Enviar correo con constancia PDF
            await EnviarCorreoConstancia(usuario, pedido);

            return RedirectToAction("Index");
        }

        // 🔹 Ver estado de envío
        public IActionResult EstadoEnvio(int id)
        {
            var pedido = _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefault(p => p.Id == id);

            if (pedido == null) return NotFound();

            return View(pedido);
        }

        // ================================
        // 🔹 Método privado para enviar correo
        // ================================
        private async Task EnviarCorreoConstancia(Usuario usuario, Pedido pedido)
        {
            var emailSettings = _config.GetSection("EmailSettings");
            var remitente = emailSettings["Email"];
            var password = emailSettings["Password"];
            var host = emailSettings["Host"];
            var puerto = int.Parse(emailSettings["Port"]);

            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress("ALODAN", remitente));
            mensaje.To.Add(MailboxAddress.Parse(usuario.Email));
            mensaje.Subject = "Constancia de compra - ALODAN";

            // ===============================
            // 🧮 Calcular subtotal, descuento y total
            // ===============================
            decimal subtotal = pedido.Detalles.Sum(d => d.PrecioUnitario * d.Cantidad);
            decimal descuento = 0;
            string porcentajeTexto = "";

            if (subtotal >= 300)
            {
                descuento = subtotal * 0.15m;
                porcentajeTexto = "15%";
            }
            else if (subtotal >= 200)
            {
                descuento = subtotal * 0.10m;
                porcentajeTexto = "10%";
            }
            else if (subtotal >= 100)
            {
                descuento = subtotal * 0.05m;
                porcentajeTexto = "5%";
            }

            decimal totalFinal = subtotal - descuento;

            // ===============================
            // 📨 Cuerpo del mensaje
            // ===============================
            var cuerpo = new BodyBuilder();

            string mensajeTexto =
                $"Hola {usuario.Nombre},\n\n" +
                $"Gracias por tu compra en ALODAN 💖.\n\n" +
                $"🧾 Resumen de tu compra:\n" +
                $"Fecha: {pedido.FechaPedido:dd/MM/yyyy}\n" +
                $"Subtotal: S/. {subtotal:N2}\n";

            if (descuento > 0)
                mensajeTexto += $"Descuento aplicado ({porcentajeTexto}): -S/. {descuento:N2}\n";

            mensajeTexto +=
                $"Total pagado: S/. {totalFinal:N2}\n\n" +
                $"Adjuntamos tu constancia de compra en formato PDF.\n\n" +
                $"Pronto recibirás más información sobre tu pedido.\n\n" +
                $"Gracias por elegir ALODAN 🌷";

            cuerpo.TextBody = mensajeTexto;

            // Generar y adjuntar PDF
            var pdfBytes = GenerarPdfConstancia(usuario, pedido);
            cuerpo.Attachments.Add("Constancia_ALODAN.pdf", pdfBytes, new ContentType("application", "pdf"));

            mensaje.Body = cuerpo.ToMessageBody();

            // ===============================
            // ✉️ Envío del correo
            // ===============================
            using (var smtp = new SmtpClient())
            {
                await smtp.ConnectAsync(host, puerto, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(remitente, password);
                await smtp.SendAsync(mensaje);
                await smtp.DisconnectAsync(true);
            }
        }

        private byte[] GenerarPdfConstancia(Usuario usuario, Pedido pedido)
        {
            using var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream))
            {
                // ===============================
                // 🧮 Calcular subtotal y descuento
                // ===============================
                decimal subtotal = pedido.Detalles.Sum(d => d.PrecioUnitario * d.Cantidad);
                decimal descuento = 0;
                string porcentajeTexto = "";

                if (subtotal >= 300)
                {
                    descuento = subtotal * 0.15m;
                    porcentajeTexto = "15%";
                }
                else if (subtotal >= 200)
                {
                    descuento = subtotal * 0.10m;
                    porcentajeTexto = "10%";
                }
                else if (subtotal >= 100)
                {
                    descuento = subtotal * 0.05m;
                    porcentajeTexto = "5%";
                }

                decimal totalFinal = subtotal - descuento;

                // ===============================
                // 🧾 Contenido del PDF
                // ===============================
                writer.WriteLine("             🖤 ALODAN - Constancia de Compra 🖤");
                writer.WriteLine("-----------------------------------------------------");
                writer.WriteLine($"Cliente: {usuario.Nombre}");
                writer.WriteLine($"Correo: {usuario.Email}");
                writer.WriteLine($"Fecha de compra: {pedido.FechaPedido:dd/MM/yyyy}");
                writer.WriteLine();
                writer.WriteLine("Detalles del pedido:");
                writer.WriteLine();

                foreach (var item in pedido.Detalles)
                {
                    var nombreProducto = item.Producto?.Nombre ?? "Producto desconocido";
                    writer.WriteLine($"- {nombreProducto} ({item.Talla}, {item.Color}) x{item.Cantidad} - S/. {item.PrecioUnitario:N2}");
                }

                writer.WriteLine();
                writer.WriteLine("-----------------------------------------------------");
                writer.WriteLine($"Subtotal: S/. {subtotal:N2}");
                if (descuento > 0)
                    writer.WriteLine($"Descuento aplicado ({porcentajeTexto}): -S/. {descuento:N2}");
                writer.WriteLine($"Total pagado: S/. {totalFinal:N2}");
                writer.WriteLine("-----------------------------------------------------");
                writer.WriteLine();
                writer.WriteLine("💌 ¡Gracias por tu compra en ALODAN!");
                writer.WriteLine("Esperamos verte pronto 💫");
                writer.Flush();
            }

            return stream.ToArray();
        }

    }
}

    


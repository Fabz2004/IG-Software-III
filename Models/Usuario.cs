using System.ComponentModel.DataAnnotations;

namespace ALODAN.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;

        public List<Pedido>? Pedidos { get; set; }

        public string Rol { get; set; } = "Cliente";
    }
}

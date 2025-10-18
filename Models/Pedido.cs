using System;
using System.Collections.Generic;

namespace ALODAN.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public DateTime FechaPedido { get; set; } = DateTime.Now;
        public decimal Total { get; set; }
        public string Estado { get; set; } = "Solicitud recibida";

   
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public int NumeroPedido { get; set; }
        public List<PedidoDetalle>? Detalles { get; set; }
    }
}

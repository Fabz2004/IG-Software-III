namespace ALODAN.Models
{
    public class CarritoItem
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public string ImagenUrl { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public string Talla { get; set; }
        public string Color { get; set; }

        public decimal Subtotal => Precio * Cantidad;
    }
}

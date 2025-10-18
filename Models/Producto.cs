using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ALODAN.Models
{

    public class Producto
    {
        public int Id { get; set; }
        
        [Required]
        public string Nombre { get; set; } = string .Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public string ImagenUrl { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Talla { get; set; } = string.Empty;


        public string Caracteristicas { get; set; } = string.Empty;


        public string Colores { get; set; } = string.Empty;
    }
}

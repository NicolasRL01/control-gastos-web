// ===== ARCHIVO: Models/TipoGasto.cs =====
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlGastosWeb.Models
{
    public class TipoGasto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public DateTime FechaCreacion { get; set; }

        public bool Activo { get; set; } = true;

        // Relaciones
        public virtual ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();
        public virtual ICollection<Presupuesto> Presupuestos { get; set; } = new List<Presupuesto>();
    }
}
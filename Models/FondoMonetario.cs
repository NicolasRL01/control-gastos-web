// ===== ARCHIVO: Models/FondoMonetario.cs =====
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlGastosWeb.Models
{
    public class FondoMonetario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Tipo { get; set; } = string.Empty; // Cuenta Bancaria, Efectivo, Ahorro

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoInicial { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoActual { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; }

        public bool Activo { get; set; } = true;

        // Relaciones
        public virtual ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();
        public virtual ICollection<Deposito> Depositos { get; set; } = new List<Deposito>();
    }
}
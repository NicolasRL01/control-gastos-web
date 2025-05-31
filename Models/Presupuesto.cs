// ===== ARCHIVO: Models/Presupuesto.cs =====
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlGastosWeb.Models
{
    public class Presupuesto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Mes { get; set; }

        [Required]
        public int Ano { get; set; }

        [Required]
        public int TipoGastoId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoPresupuestado { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoEjecutado { get; set; } = 0;

        [Required]
        public DateTime FechaCreacion { get; set; }

        public bool Activo { get; set; } = true;

        // Propiedades calculadas
        [NotMapped]
        public decimal MontoRestante => MontoPresupuestado - MontoEjecutado;

        [NotMapped]
        public double PorcentajeEjecucion => MontoPresupuestado > 0 ? (double)(MontoEjecutado / MontoPresupuestado * 100) : 0;

        [NotMapped]
        public bool EstaSobrepasado => MontoEjecutado > MontoPresupuestado;

        // Relaciones
        [ForeignKey("TipoGastoId")]
        public virtual TipoGasto TipoGasto { get; set; } = null!;
    }
}
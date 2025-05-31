// ===== ARCHIVO: Models/Gasto.cs ACTUALIZADO =====
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlGastosWeb.Models
{
    public class Gasto
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "La fecha del gasto es obligatoria")]
        [Display(Name = "Fecha del Gasto")]
        [DataType(DataType.Date)]
        public DateTime FechaGasto { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(200, ErrorMessage = "La descripción no puede exceder 200 caracteres")]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto es obligatorio")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Monto")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor que cero")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "El nombre del comercio es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre del comercio no puede exceder 100 caracteres")]
        [Display(Name = "Nombre del Comercio")]
        public string NombreComercio { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de documento es obligatorio")]
        [Display(Name = "Tipo de Documento")]
        public string TipoDocumento { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "El número de documento no puede exceder 50 caracteres")]
        [Display(Name = "Número de Documento")]
        public string? NumeroDocumento { get; set; }

        [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres")]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        // Relaciones
        [Required(ErrorMessage = "El tipo de gasto es obligatorio")]
        [Display(Name = "Categoría")]
        public int TipoGastoId { get; set; }

        [Required(ErrorMessage = "El fondo monetario es obligatorio")]
        [Display(Name = "Fondo Monetario")]
        public int FondoMonetarioId { get; set; }

        // Campos de auditoría
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public bool Activo { get; set; } = true;

        // Propiedades de navegación
        [ForeignKey("TipoGastoId")]
        public virtual TipoGasto TipoGasto { get; set; } = null!;

        [ForeignKey("FondoMonetarioId")]
        public virtual FondoMonetario FondoMonetario { get; set; } = null!;
    }
}
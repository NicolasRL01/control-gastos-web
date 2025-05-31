// ===== ARCHIVO: Models/ApplicationDbContext.cs =====
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace ControlGastosWeb.Models
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<TipoGasto> TiposGasto { get; set; }
        public DbSet<FondoMonetario> FondosMonetarios { get; set; }
        public DbSet<Presupuesto> Presupuestos { get; set; }
        public DbSet<Gasto> Gastos { get; set; }
        public DbSet<Deposito> Depositos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuraciones adicionales
            modelBuilder.Entity<TipoGasto>()
                .HasIndex(t => t.Codigo)
                .IsUnique();

            modelBuilder.Entity<Presupuesto>()
                .HasIndex(p => new { p.Mes, p.Ano, p.TipoGastoId })
                .IsUnique();

            // Datos iniciales
            modelBuilder.Entity<TipoGasto>().HasData(
                new TipoGasto { Id = 1, Codigo = "TG001", Nombre = "Alimentación", FechaCreacion = new DateTime(2024, 1, 1) },
                new TipoGasto { Id = 2, Codigo = "TG002", Nombre = "Transporte", FechaCreacion = new DateTime(2024, 1, 1) },
                new TipoGasto { Id = 3, Codigo = "TG003", Nombre = "Entretenimiento", FechaCreacion = new DateTime(2024, 1, 1) },
                new TipoGasto { Id = 4, Codigo = "TG004", Nombre = "Servicios Públicos", FechaCreacion = new DateTime(2024, 1, 1) },
                new TipoGasto { Id = 5, Codigo = "TG005", Nombre = "Salud", FechaCreacion = new DateTime(2024, 1, 1) }
            );

            modelBuilder.Entity<FondoMonetario>().HasData(
                new FondoMonetario
                {
                    Id = 1,
                    Nombre = "Cuenta Corriente Principal",
                    Tipo = "Cuenta Bancaria",
                    SaldoInicial = 1000000,
                    SaldoActual = 1000000,
                    FechaCreacion = new DateTime(2024, 1, 1)
                },
                new FondoMonetario
                {
                    Id = 2,
                    Nombre = "Efectivo",
                    Tipo = "Efectivo",
                    SaldoInicial = 200000,
                    SaldoActual = 200000,
                    FechaCreacion = new DateTime(2024, 1, 1)
                }
            );
        }
    }
}
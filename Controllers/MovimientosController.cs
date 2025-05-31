using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlGastosWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ControlGastosWeb.Controllers
{
    [Authorize]
    public class MovimientosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MovimientosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Movimientos
        public async Task<IActionResult> Index(DateTime? fechaInicio, DateTime? fechaFin, int? fondoId, string tipoMovimiento = "")
        {
            Console.WriteLine("=== CONSULTA DE MOVIMIENTOS ===");

            // Si no se proporcionan fechas, usar el mes actual
            if (!fechaInicio.HasValue)
            {
                fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (!fechaFin.HasValue)
            {
                fechaFin = fechaInicio.Value.AddMonths(1).AddDays(-1);
            }

            Console.WriteLine($"Período: {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}");
            Console.WriteLine($"Fondo seleccionado: {fondoId}");
            Console.WriteLine($"Tipo movimiento: {tipoMovimiento}");

            try
            {
                // DEBUG: Contar total de gastos en la BD
                var totalGastosBD = await _context.Gastos.CountAsync();
                var totalDepositosBD = await _context.Depositos.CountAsync();
                Console.WriteLine($"DEBUG: Total gastos en BD: {totalGastosBD}");
                Console.WriteLine($"DEBUG: Total depósitos en BD: {totalDepositosBD}");

                // Obtener todos los movimientos (gastos y depósitos)
                var movimientos = new List<MovimientoViewModel>();

                // 1. OBTENER GASTOS
                var gastosQuery = _context.Gastos
                    .Include(g => g.TipoGasto)
                    .Include(g => g.FondoMonetario)
                    .Where(g => g.Activo &&
                               g.FechaGasto >= fechaInicio &&
                               g.FechaGasto <= fechaFin);

                Console.WriteLine($"DEBUG: Query gastos - FechaInicio: {fechaInicio}, FechaFin: {fechaFin}");

                // Filtrar por fondo si se especifica
                if (fondoId.HasValue && fondoId > 0)
                {
                    gastosQuery = gastosQuery.Where(g => g.FondoMonetarioId == fondoId);
                    Console.WriteLine($"DEBUG: Filtrando por fondo: {fondoId}");
                }

                var gastos = await gastosQuery.ToListAsync();
                Console.WriteLine($"DEBUG: Gastos encontrados en el rango: {gastos.Count}");

                // DEBUG: Mostrar algunos gastos de ejemplo
                foreach (var gasto in gastos.Take(3))
                {
                    Console.WriteLine($"DEBUG: Gasto - Fecha: {gasto.FechaGasto:dd/MM/yyyy}, Monto: ${gasto.Monto}, Descripción: {gasto.Descripcion}");
                }

                // Convertir gastos a MovimientoViewModel
                foreach (var gasto in gastos)
                {
                    movimientos.Add(new MovimientoViewModel
                    {
                        Id = gasto.Id,
                        Fecha = gasto.FechaGasto,
                        Descripcion = gasto.Descripcion,
                        TipoMovimiento = "Gasto",
                        Categoria = gasto.TipoGasto?.Nombre ?? "Sin categoría",
                        FondoMonetario = gasto.FondoMonetario?.Nombre ?? "Sin fondo",
                        Monto = gasto.Monto,
                        EsGasto = true,
                        Observaciones = gasto.Observaciones ?? "",
                        NombreComercio = gasto.NombreComercio ?? "",
                        TipoDocumento = gasto.TipoDocumento ?? "",
                        NumeroDocumento = gasto.NumeroDocumento ?? ""
                    });
                }

                // 2. OBTENER DEPÓSITOS
                var depositosQuery = _context.Depositos
                    .Include(d => d.FondoMonetario)
                    .Where(d => d.Activo &&
                               d.Fecha >= fechaInicio &&
                               d.Fecha <= fechaFin);

                // Filtrar por fondo si se especifica
                if (fondoId.HasValue && fondoId > 0)
                {
                    depositosQuery = depositosQuery.Where(d => d.FondoMonetarioId == fondoId);
                }

                var depositos = await depositosQuery.ToListAsync();
                Console.WriteLine($"DEBUG: Depósitos encontrados en el rango: {depositos.Count}");

                // Convertir depósitos a MovimientoViewModel
                foreach (var deposito in depositos)
                {
                    movimientos.Add(new MovimientoViewModel
                    {
                        Id = deposito.Id,
                        Fecha = deposito.Fecha,
                        Descripcion = deposito.Descripcion ?? "Depósito",
                        TipoMovimiento = "Depósito",
                        Categoria = "Depósito",
                        FondoMonetario = deposito.FondoMonetario?.Nombre ?? "Sin fondo",
                        Monto = deposito.Monto,
                        EsGasto = false,
                        Observaciones = deposito.Descripcion ?? ""
                    });
                }

                // 3. FILTRAR POR TIPO DE MOVIMIENTO SI SE ESPECIFICA
                if (!string.IsNullOrEmpty(tipoMovimiento))
                {
                    movimientos = movimientos.Where(m => m.TipoMovimiento == tipoMovimiento).ToList();
                    Console.WriteLine($"DEBUG: Filtrado por tipo '{tipoMovimiento}', quedaron: {movimientos.Count}");
                }

                // 4. ORDENAR POR FECHA DESCENDENTE
                movimientos = movimientos.OrderByDescending(m => m.Fecha).ToList();

                Console.WriteLine($"DEBUG: Total movimientos finales: {movimientos.Count}");

                // 5. CALCULAR TOTALES
                var totalGastos = movimientos.Where(m => m.EsGasto).Sum(m => m.Monto);
                var totalDepositos = movimientos.Where(m => !m.EsGasto).Sum(m => m.Monto);
                var balance = totalDepositos - totalGastos;

                Console.WriteLine($"DEBUG: Total gastos calculado: ${totalGastos}");
                Console.WriteLine($"DEBUG: Total depósitos calculado: ${totalDepositos}");

                ViewBag.TotalGastos = totalGastos;
                ViewBag.TotalDepositos = totalDepositos;
                ViewBag.Balance = balance;
                ViewBag.CantidadMovimientos = movimientos.Count;

                // 6. DATOS PARA LOS FILTROS
                ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
                ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
                ViewBag.FondoSeleccionado = fondoId;
                ViewBag.TipoMovimientoSeleccionado = tipoMovimiento;

                // Lista de fondos para el filtro
                var fondos = await _context.FondosMonetarios
                    .Where(f => f.Activo)
                    .OrderBy(f => f.Nombre)
                    .ToListAsync();
                ViewBag.Fondos = fondos;

                Console.WriteLine($"DEBUG: Fondos cargados: {fondos.Count}");

                return View(movimientos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR en consulta: {ex.Message}");
                Console.WriteLine($"ERROR Stack trace: {ex.StackTrace}");
                ViewBag.TotalGastos = 0m;
                ViewBag.TotalDepositos = 0m;
                ViewBag.Balance = 0m;
                ViewBag.CantidadMovimientos = 0;
                ViewBag.Fondos = new List<FondoMonetario>();
                return View(new List<MovimientoViewModel>());
            }
        }

        // Método para exportar a Excel (opcional)
        public async Task<IActionResult> ExportarExcel(DateTime? fechaInicio, DateTime? fechaFin, int? fondoId, string tipoMovimiento = "")
        {
            // Implementar exportación a Excel si es necesario
            // Por ahora, redirigir a la consulta normal
            return RedirectToAction("Index", new { fechaInicio, fechaFin, fondoId, tipoMovimiento });
        }

        // GET: Movimientos/Grafico
        public async Task<IActionResult> Grafico(DateTime? fechaInicio, DateTime? fechaFin)
        {
            Console.WriteLine("=== GRÁFICO COMPARATIVO DE PRESUPUESTOS ===");

            // Si no se proporcionan fechas, usar el mes actual
            if (!fechaInicio.HasValue)
            {
                fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (!fechaFin.HasValue)
            {
                fechaFin = fechaInicio.Value.AddMonths(1).AddDays(-1);
            }

            Console.WriteLine($"Período del gráfico: {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}");

            try
            {
                var datosGrafico = new List<DatoGraficoViewModel>();

                // DEBUG: Verificar presupuestos en la BD
                var totalPresupuestosBD = await _context.Presupuestos.CountAsync();
                Console.WriteLine($"DEBUG: Total presupuestos en BD: {totalPresupuestosBD}");

                // DEBUG: Mostrar algunos presupuestos de ejemplo
                var presupuestosEjemplo = await _context.Presupuestos
                    .Include(p => p.TipoGasto)
                    .Take(5)
                    .ToListAsync();

                Console.WriteLine("DEBUG: Presupuestos de ejemplo en la BD:");
                foreach (var pej in presupuestosEjemplo)
                {
                    Console.WriteLine($"DEBUG: - {pej.TipoGasto?.Nombre ?? "SIN TIPO"} | Mes: {pej.Mes} | Año: {pej.Ano} | Monto: ${pej.MontoPresupuestado} | Activo: {pej.Activo}");
                }

                // Obtener todos los tipos de gasto activos
                var tiposGasto = await _context.TiposGasto
                    .Where(t => t.Activo)
                    .OrderBy(t => t.Nombre)
                    .ToListAsync();

                Console.WriteLine($"DEBUG: Tipos de gasto encontrados: {tiposGasto.Count}");

                foreach (var tipoGasto in tiposGasto)
                {
                    Console.WriteLine($"DEBUG: Procesando tipo de gasto: {tipoGasto.Nombre}");

                    // Calcular presupuestado en el rango de fechas
                    decimal totalPresupuestado = 0;

                    // Obtener todos los meses en el rango
                    var fechaActual = new DateTime(fechaInicio.Value.Year, fechaInicio.Value.Month, 1);
                    var fechaFinal = new DateTime(fechaFin.Value.Year, fechaFin.Value.Month, 1);

                    while (fechaActual <= fechaFinal)
                    {
                        Console.WriteLine($"DEBUG: Buscando presupuesto para {tipoGasto.Nombre} en {fechaActual:MM/yyyy}");

                        var presupuesto = await _context.Presupuestos
                            .FirstOrDefaultAsync(p => p.TipoGastoId == tipoGasto.Id &&
                                                     p.Mes == fechaActual.Month &&
                                                     p.Ano == fechaActual.Year &&
                                                     p.Activo);

                        if (presupuesto != null)
                        {
                            Console.WriteLine($"DEBUG: Presupuesto encontrado: ${presupuesto.MontoPresupuestado}");
                            totalPresupuestado += presupuesto.MontoPresupuestado;
                        }
                        else
                        {
                            Console.WriteLine($"DEBUG: No se encontró presupuesto para {tipoGasto.Nombre} en {fechaActual:MM/yyyy}");
                        }

                        fechaActual = fechaActual.AddMonths(1);
                    }

                    // Calcular ejecutado en el rango de fechas
                    var totalEjecutado = await _context.Gastos
                        .Where(g => g.TipoGastoId == tipoGasto.Id &&
                                   g.FechaGasto >= fechaInicio &&
                                   g.FechaGasto <= fechaFin &&
                                   g.Activo)
                        .SumAsync(g => g.Monto);

                    Console.WriteLine($"DEBUG: {tipoGasto.Nombre} - Presupuestado: ${totalPresupuestado}, Ejecutado: ${totalEjecutado}");

                    // Agregar TODOS los tipos de gasto, incluso si están en $0
                    datosGrafico.Add(new DatoGraficoViewModel
                    {
                        TipoGasto = tipoGasto.Nombre,
                        MontoPresupuestado = totalPresupuestado,
                        MontoEjecutado = totalEjecutado,
                        PorcentajeEjecucion = totalPresupuestado > 0 ? (totalEjecutado / totalPresupuestado * 100) : 0
                    });
                }

                Console.WriteLine($"DEBUG: Datos del gráfico generados: {datosGrafico.Count}");

                // Calcular totales generales
                var totalPresupuestadoGeneral = datosGrafico.Sum(d => d.MontoPresupuestado);
                var totalEjecutadoGeneral = datosGrafico.Sum(d => d.MontoEjecutado);

                ViewBag.TotalPresupuestado = totalPresupuestadoGeneral;
                ViewBag.TotalEjecutado = totalEjecutadoGeneral;
                ViewBag.PorcentajeEjecucionGeneral = totalPresupuestadoGeneral > 0 ?
                    (totalEjecutadoGeneral / totalPresupuestadoGeneral * 100) : 0;

                ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
                ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");

                Console.WriteLine($"DEBUG: Total general - Presupuestado: ${totalPresupuestadoGeneral}, Ejecutado: ${totalEjecutadoGeneral}");

                return View(datosGrafico);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR generando gráfico: {ex.Message}");
                Console.WriteLine($"ERROR Stack trace: {ex.StackTrace}");
                return View(new List<DatoGraficoViewModel>());
            }
        }
    }

    // ViewModel para mostrar los movimientos unificados
    public class MovimientoViewModel
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; } = "";
        public string TipoMovimiento { get; set; } = ""; // "Gasto" o "Depósito"
        public string Categoria { get; set; } = "";
        public string FondoMonetario { get; set; } = "";
        public decimal Monto { get; set; }
        public bool EsGasto { get; set; }
        public string Observaciones { get; set; } = "";
        public string NombreComercio { get; set; } = "";
        public string TipoDocumento { get; set; } = "";
        public string NumeroDocumento { get; set; } = "";

        public string MontoFormateado => EsGasto ? $"-${Monto:N2}" : $"+${Monto:N2}";
        public string CssClass => EsGasto ? "text-danger" : "text-success";
    }

    // ViewModel para el gráfico comparativo
    public class DatoGraficoViewModel
    {
        public string TipoGasto { get; set; } = "";
        public decimal MontoPresupuestado { get; set; }
        public decimal MontoEjecutado { get; set; }
        public decimal PorcentajeEjecucion { get; set; }
        public decimal Diferencia => MontoEjecutado - MontoPresupuestado;
        public bool EstaSobrepasado => MontoEjecutado > MontoPresupuestado;
        public string ColorBarra => EstaSobrepasado ? "#dc3545" : "#28a745"; // Rojo si sobrepasado, verde si normal
    }
}
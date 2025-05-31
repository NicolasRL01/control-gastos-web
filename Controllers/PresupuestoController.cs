using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlGastosWeb.Data;
using ControlGastosWeb.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ControlGastosWeb.Controllers
{
    public class PresupuestoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PresupuestoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Presupuesto
        public async Task<IActionResult> Index()
        {
            var presupuestos = await _context.Presupuestos
                .Include(p => p.TipoGasto)
                .OrderByDescending(p => p.Periodo)
                .ToListAsync();

            // Calcular totales y estadísticas
            var totalPresupuestos = presupuestos.Count;
            var totalPresupuestado = presupuestos.Sum(p => p.MontoPresupuestado);

            // Calcular total ejecutado (gastos reales)
            var totalEjecutado = 0m;
            foreach (var presupuesto in presupuestos)
            {
                var gastosDelPresupuesto = await _context.Gastos
                    .Where(g => g.TipoGastoId == presupuesto.TipoGastoId &&
                               g.Fecha.Year == presupuesto.Periodo.Year &&
                               g.Fecha.Month == presupuesto.Periodo.Month)
                    .SumAsync(g => g.Monto);
                totalEjecutado += gastosDelPresupuesto;
            }

            var totalRestante = totalPresupuestado - totalEjecutado;

            ViewBag.TotalPresupuestos = totalPresupuestos;
            ViewBag.TotalPresupuestado = totalPresupuestado;
            ViewBag.TotalEjecutado = totalEjecutado;
            ViewBag.TotalRestante = totalRestante;

            return View(presupuestos);
        }

        // GET: Presupuesto/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var presupuesto = await _context.Presupuestos
                .Include(p => p.TipoGasto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (presupuesto == null)
            {
                return NotFound();
            }

            return View(presupuesto);
        }

        // GET: Presupuesto/Create
        public IActionResult Create()
        {
            ViewData["TipoGastoId"] = new SelectList(_context.TipoGastos, "Id", "Nombre");
            return View();
        }

        // POST: Presupuesto/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,TipoGastoId,MontoPresupuestado,Periodo")] Presupuesto presupuesto)
        {
            if (ModelState.IsValid)
            {
                // Verificar si ya existe un presupuesto para ese tipo de gasto y período
                var existePresupuesto = await _context.Presupuestos
                    .AnyAsync(p => p.TipoGastoId == presupuesto.TipoGastoId &&
                                  p.Periodo.Year == presupuesto.Periodo.Year &&
                                  p.Periodo.Month == presupuesto.Periodo.Month);

                if (existePresupuesto)
                {
                    TempData["ErrorMessage"] = "Ya existe un presupuesto para este tipo de gasto en el período seleccionado.";
                    ViewData["TipoGastoId"] = new SelectList(_context.TipoGastos, "Id", "Nombre", presupuesto.TipoGastoId);
                    return View(presupuesto);
                }

                _context.Add(presupuesto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Presupuesto creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["TipoGastoId"] = new SelectList(_context.TipoGastos, "Id", "Nombre", presupuesto.TipoGastoId);
            return View(presupuesto);
        }

        // GET: Presupuesto/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var presupuesto = await _context.Presupuestos.FindAsync(id);
            if (presupuesto == null)
            {
                return NotFound();
            }
            ViewData["TipoGastoId"] = new SelectList(_context.TipoGastos, "Id", "Nombre", presupuesto.TipoGastoId);
            return View(presupuesto);
        }

        // POST: Presupuesto/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TipoGastoId,MontoPresupuestado,Periodo")] Presupuesto presupuesto)
        {
            if (id != presupuesto.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(presupuesto);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Presupuesto actualizado exitosamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PresupuestoExists(presupuesto.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["TipoGastoId"] = new SelectList(_context.TipoGastos, "Id", "Nombre", presupuesto.TipoGastoId);
            return View(presupuesto);
        }

        // GET: Presupuesto/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var presupuesto = await _context.Presupuestos
                .Include(p => p.TipoGasto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (presupuesto == null)
            {
                return NotFound();
            }

            return View(presupuesto);
        }

        // POST: Presupuesto/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var presupuesto = await _context.Presupuestos.FindAsync(id);
            if (presupuesto != null)
            {
                _context.Presupuestos.Remove(presupuesto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Presupuesto eliminado exitosamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Presupuesto/Reporte
        [HttpGet]
        public async Task<IActionResult> Reporte()
        {
            return View();
        }

        // POST: Presupuesto/Reporte
        [HttpPost]
        public async Task<IActionResult> Reporte(DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                // Si no se proporcionan fechas, usar el mes actual
                if (!fechaInicio.HasValue || !fechaFin.HasValue)
                {
                    var now = DateTime.Now;
                    fechaInicio = new DateTime(now.Year, now.Month, 1);
                    fechaFin = fechaInicio.Value.AddMonths(1).AddDays(-1);
                }

                // Obtener presupuestos del período
                var presupuestos = await _context.Presupuestos
                    .Include(p => p.TipoGasto)
                    .Where(p => p.Periodo.Year == fechaInicio.Value.Year && 
                               p.Periodo.Month >= fechaInicio.Value.Month &&
                               p.Periodo.Month <= fechaFin.Value.Month)
                    .ToListAsync();

                // Obtener gastos del período
                var gastos = await _context.Gastos
                    .Include(g => g.TipoGasto)
                    .Where(g => g.Fecha >= fechaInicio.Value && g.Fecha <= fechaFin.Value)
                    .ToListAsync();

                // Calcular totales
                var totalPresupuestado = presupuestos.Sum(p => p.MontoPresupuestado);
                var totalEjecutado = gastos.Sum(g => g.Monto);

                // Datos por tipo de gasto
                var datosPorTipo = presupuestos.Select(p => new
                {
                    TipoGasto = p.TipoGasto.Nombre,
                    Presupuestado = p.MontoPresupuestado,
                    Ejecutado = gastos.Where(g => g.TipoGastoId == p.TipoGastoId).Sum(g => g.Monto)
                }).ToList();

                // ViewBag para la vista
                ViewBag.FechaInicio = fechaInicio;
                ViewBag.FechaFin = fechaFin;
                ViewBag.TotalPresupuestado = totalPresupuestado;
                ViewBag.TotalEjecutado = totalEjecutado;
                ViewBag.DatosPorTipo = datosPorTipo;
                ViewBag.TieneDatos = datosPorTipo.Any();

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al generar el reporte: " + ex.Message;
                return View();
            }
        }

        // MÉTODO PARA AJAX - Obtener datos del gráfico
        [HttpGet]
        public async Task<IActionResult> ObtenerDatosGrafico(DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                // Si no se proporcionan fechas, usar el mes actual
                if (!fechaInicio.HasValue || !fechaFin.HasValue)
                {
                    var now = DateTime.Now;
                    fechaInicio = new DateTime(now.Year, now.Month, 1);
                    fechaFin = fechaInicio.Value.AddMonths(1).AddDays(-1);
                }

                // Obtener presupuestos del período
                var presupuestos = await _context.Presupuestos
                    .Include(p => p.TipoGasto)
                    .Where(p => p.Periodo.Year == fechaInicio.Value.Year && 
                               p.Periodo.Month >= fechaInicio.Value.Month &&
                               p.Periodo.Month <= fechaFin.Value.Month)
                    .ToListAsync();

                // Obtener gastos del período
                var gastos = await _context.Gastos
                    .Include(g => g.TipoGasto)
                    .Where(g => g.Fecha >= fechaInicio.Value && g.Fecha <= fechaFin.Value)
                    .ToListAsync();

                // Datos para el gráfico
                var datosGrafico = presupuestos.Select(p => new
                {
                    tipoGasto = p.TipoGasto.Nombre,
                    presupuestado = p.MontoPresupuestado,
                    ejecutado = gastos.Where(g => g.TipoGastoId == p.TipoGastoId).Sum(g => g.Monto)
                }).ToList();

                return Json(new { 
                    success = true, 
                    datos = datosGrafico,
                    totalPresupuestado = presupuestos.Sum(p => p.MontoPresupuestado),
                    totalEjecutado = gastos.Sum(g => g.Monto)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private bool PresupuestoExists(int id)
        {
            return _context.Presupuestos.Any(e => e.Id == id);
        }
    }
}

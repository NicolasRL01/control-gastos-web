using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlGastosWeb.Data;
using ControlGastosWeb.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;

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
            try
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
            catch (Exception ex)
            {
                // Log del error (considera usar ILogger)
                TempData["ErrorMessage"] = "Error al cargar presupuestos: " + ex.Message;
                return View(new List<Presupuesto>());
            }
        }

        // GET: Presupuesto/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var presupuesto = await _context.Presupuestos
                    .Include(p => p.TipoGasto)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (presupuesto == null)
                {
                    return NotFound();
                }

                return View(presupuesto);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cargar el presupuesto: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Presupuesto/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewData["TipoGastoId"] = new SelectList(await _context.TipoGastos.ToListAsync(), "Id", "Nombre");
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cargar formulario: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Presupuesto/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,TipoGastoId,MontoPresupuestado,Periodo")] Presupuesto presupuesto)
        {
            try
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
                        ViewData["TipoGastoId"] = new SelectList(await _context.TipoGastos.ToListAsync(), "Id", "Nombre", presupuesto.TipoGastoId);
                        return View(presupuesto);
                    }

                    _context.Add(presupuesto);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Presupuesto creado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                ViewData["TipoGastoId"] = new SelectList(await _context.TipoGastos.ToListAsync(), "Id", "Nombre", presupuesto.TipoGastoId);
                return View(presupuesto);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al crear presupuesto: " + ex.Message;
                ViewData["TipoGastoId"] = new SelectList(await _context.TipoGastos.ToListAsync(), "Id", "Nombre", presupuesto.TipoGastoId);
                return View(presupuesto);
            }
        }

        // GET: Presupuesto/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var presupuesto = await _context.Presupuestos.FindAsync(id);
                if (presupuesto == null)
                {
                    return NotFound();
                }
                ViewData["TipoGastoId"] = new SelectList(await _context.TipoGastos.ToListAsync(), "Id", "Nombre", presupuesto.TipoGastoId);
                return View(presupuesto);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cargar el presupuesto: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
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

            try
            {
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
                        if (!await PresupuestoExistsAsync(presupuesto.Id))
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
                ViewData["TipoGastoId"] = new SelectList(await _context.TipoGastos.ToListAsync(), "Id", "Nombre", presupuesto.TipoGastoId);
                return View(presupuesto);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al actualizar presupuesto: " + ex.Message;
                ViewData["TipoGastoId"] = new SelectList(await _context.TipoGastos.ToListAsync(), "Id", "Nombre", presupuesto.TipoGastoId);
                return View(presupuesto);
            }
        }

        // GET: Presupuesto/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var presupuesto = await _context.Presupuestos
                    .Include(p => p.TipoGasto)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (presupuesto == null)
                {
                    return NotFound();
                }

                return View(presupuesto);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cargar el presupuesto: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Presupuesto/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al eliminar presupuesto: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Presupuesto/Reporte
        [HttpGet]
        public async Task<IActionResult> Reporte()
        {
            return View();
        }

        // MÉTODO PRINCIPAL PARA AJAX - Obtener datos del gráfico
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

                // Validar que fechaInicio no sea mayor que fechaFin
                if (fechaInicio > fechaFin)
                {
                    return Json(new { success = false, message = "La fecha de inicio no puede ser mayor que la fecha fin." });
                }

                // Obtener todos los presupuestos que están en el rango de fechas
                var presupuestos = await _context.Presupuestos
                    .Include(p => p.TipoGasto)
                    .Where(p => p.Periodo >= fechaInicio.Value && p.Periodo <= fechaFin.Value)
                    .ToListAsync();

                // Si no hay presupuestos, devolver vacío
                if (!presupuestos.Any())
                {
                    return Json(new { 
                        success = true, 
                        datos = new List<object>(),
                        totalPresupuestado = 0,
                        totalEjecutado = 0,
                        mensaje = "No se encontraron presupuestos para el período seleccionado."
                    });
                }

                // Obtener gastos del período
                var gastos = await _context.Gastos
                    .Include(g => g.TipoGasto)
                    .Where(g => g.Fecha >= fechaInicio.Value && g.Fecha <= fechaFin.Value)
                    .ToListAsync();

                // Datos para el gráfico - agrupado por tipo de gasto
                var datosGrafico = presupuestos
                    .GroupBy(p => new { p.TipoGastoId, p.TipoGasto.Nombre })
                    .Select(g => new
                    {
                        tipoGasto = g.Key.Nombre,
                        presupuestado = g.Sum(p => p.MontoPresupuestado),
                        ejecutado = gastos.Where(gasto => gasto.TipoGastoId == g.Key.TipoGastoId).Sum(gasto => gasto.Monto),
                        porcentajeEjecucion = g.Sum(p => p.MontoPresupuestado) > 0 ? 
                            (gastos.Where(gasto => gasto.TipoGastoId == g.Key.TipoGastoId).Sum(gasto => gasto.Monto) / g.Sum(p => p.MontoPresupuestado) * 100) : 0
                    })
                    .OrderBy(x => x.tipoGasto)
                    .ToList();

                var totalPresupuestado = presupuestos.Sum(p => p.MontoPresupuestado);
                var totalEjecutado = gastos.Sum(g => g.Monto);

                return Json(new { 
                    success = true, 
                    datos = datosGrafico,
                    totalPresupuestado = totalPresupuestado,
                    totalEjecutado = totalEjecutado
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al obtener datos: " + ex.Message });
            }
        }

        private async Task<bool> PresupuestoExistsAsync(int id)
        {
            return await _context.Presupuestos.AnyAsync(e => e.Id == id);
        }
    }
}

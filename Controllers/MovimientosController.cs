using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlGastosWeb.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace ControlGastosWeb.Controllers;

[Authorize] // Requiere autenticación para todo el controller
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
                .OrderByDescending(p => p.Ano)
                .ThenByDescending(p => p.Mes)
                .ToListAsync();

            var totalPresupuestos = presupuestos.Count;
            var totalPresupuestado = presupuestos.Sum(p => p.MontoPresupuestado);

            decimal totalEjecutado = 0;
            foreach (var presupuesto in presupuestos)
            {
                var gastosDelPresupuesto = await _context.Gastos
                    .Where(g => g.TipoGastoId == presupuesto.TipoGastoId &&
                               g.FechaGasto.Year == presupuesto.Ano &&
                               g.FechaGasto.Month == presupuesto.Mes)
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
            var tipoGastos = await _context.TiposGasto.ToListAsync();
            ViewData["TipoGastoId"] = new SelectList(tipoGastos, "Id", "Nombre");
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
    public async Task<IActionResult> Create([Bind("Id,Mes,Ano,TipoGastoId,MontoPresupuestado")] Presupuesto presupuesto)
    {
        try
        {
            // Debug: Verificar qué datos llegan
            System.Diagnostics.Debug.WriteLine($"Datos recibidos - Mes: {presupuesto.Mes}, Año: {presupuesto.Ano}, TipoGastoId: {presupuesto.TipoGastoId}, Monto: {presupuesto.MontoPresupuestado}");
            
            // Verificar errores de validación
            if (!ModelState.IsValid)
            {
                // Debug: Mostrar errores de validación
                foreach (var error in ModelState)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                
                TempData["ErrorMessage"] = "Hay errores en el formulario. Por favor, revise los datos ingresados.";
                var tipoGastos = await _context.TiposGasto.ToListAsync();
                ViewData["TipoGastoId"] = new SelectList(tipoGastos, "Id", "Nombre", presupuesto.TipoGastoId);
                return View(presupuesto);
            }

            // Validar que el mes esté en rango válido
            if (presupuesto.Mes < 1 || presupuesto.Mes > 12)
            {
                TempData["ErrorMessage"] = "El mes debe estar entre 1 y 12.";
                var tipoGastos = await _context.TiposGasto.ToListAsync();
                ViewData["TipoGastoId"] = new SelectList(tipoGastos, "Id", "Nombre", presupuesto.TipoGastoId);
                return View(presupuesto);
            }

            // Validar que el año sea razonable
            if (presupuesto.Ano < 2020 || presupuesto.Ano > 2030)
            {
                TempData["ErrorMessage"] = "El año debe estar entre 2020 y 2030.";
                var tipoGastos = await _context.TiposGasto.ToListAsync();
                ViewData["TipoGastoId"] = new SelectList(tipoGastos, "Id", "Nombre", presupuesto.TipoGastoId);
                return View(presupuesto);
            }

            // Verificar si ya existe un presupuesto para ese tipo de gasto y período
            var existePresupuesto = await _context.Presupuestos
                .AnyAsync(p => p.TipoGastoId == presupuesto.TipoGastoId &&
                              p.Ano == presupuesto.Ano &&
                              p.Mes == presupuesto.Mes);

            if (existePresupuesto)
            {
                TempData["ErrorMessage"] = "Ya existe un presupuesto para este tipo de gasto en el período seleccionado.";
                var tipoGastos = await _context.TiposGasto.ToListAsync();
                ViewData["TipoGastoId"] = new SelectList(tipoGastos, "Id", "Nombre", presupuesto.TipoGastoId);
                return View(presupuesto);
            }

            // Establecer valores por defecto
            presupuesto.FechaCreacion = DateTime.Now;
            presupuesto.Activo = true;
            presupuesto.MontoEjecutado = 0;

            // Intentar guardar
            _context.Add(presupuesto);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Presupuesto creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            // Debug: Mostrar error completo
            System.Diagnostics.Debug.WriteLine($"Error al crear presupuesto: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            TempData["ErrorMessage"] = "Error al crear presupuesto: " + ex.Message;
            var tipoGastos = await _context.TiposGasto.ToListAsync();
            ViewData["TipoGastoId"] = new SelectList(tipoGastos, "Id", "Nombre", presupuesto.TipoGastoId);
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

            var tipoGastos = await _context.TiposGasto.ToListAsync();
            ViewData["TipoGastoId"] = new SelectList(tipoGastos, "Id", "Nombre", presupuesto.TipoGastoId);
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
    public async Task<IActionResult> Edit(int id, [Bind("Id,Mes,Ano,TipoGastoId,MontoPresupuestado,FechaCreacion,Activo")] Presupuesto presupuesto)
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

            var tipoGastos = await _context.TiposGasto.ToListAsync();
            ViewData["TipoGastoId"] = new SelectList(tipoGastos, "Id", "Nombre", presupuesto.TipoGastoId);
            return View(presupuesto);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Error al actualizar presupuesto: " + ex.Message;
            var tipoGastos = await _context.TiposGasto.ToListAsync();
            ViewData["TipoGastoId"] = new SelectList(tipoGastos, "Id", "Nombre", presupuesto.TipoGastoId);
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
    public IActionResult Reporte()
    {
        return View();
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

            // Validar que fechaInicio no sea mayor que fechaFin
            if (fechaInicio > fechaFin)
            {
                return Json(new { success = false, message = "La fecha de inicio no puede ser mayor que la fecha fin." });
            }

            // Extraer rango de años y meses
            var anoInicio = fechaInicio.Value.Year;
            var mesInicio = fechaInicio.Value.Month;
            var anoFin = fechaFin.Value.Year;
            var mesFin = fechaFin.Value.Month;

            // Obtener presupuestos del período
            var presupuestos = await _context.Presupuestos
                .Include(p => p.TipoGasto)
                .Where(p => (p.Ano > anoInicio || (p.Ano == anoInicio && p.Mes >= mesInicio)) &&
                           (p.Ano < anoFin || (p.Ano == anoFin && p.Mes <= mesFin)))
                .ToListAsync();

            // Si no hay presupuestos, devolver vacío
            if (presupuestos.Count == 0)
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
                .Where(g => g.FechaGasto >= fechaInicio.Value && g.FechaGasto <= fechaFin.Value)
                .ToListAsync();

            // Crear datos para el gráfico agrupando por tipo de gasto
            var datosGrafico = presupuestos
                .GroupBy(p => new { p.TipoGastoId, TipoGastoNombre = p.TipoGasto.Nombre })
                .Select(g => new
                {
                    tipoGasto = g.Key.TipoGastoNombre,
                    presupuestado = g.Sum(p => p.MontoPresupuestado),
                    ejecutado = gastos.Where(gasto => gasto.TipoGastoId == g.Key.TipoGastoId).Sum(gasto => gasto.Monto)
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

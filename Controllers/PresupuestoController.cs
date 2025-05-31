using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlGastosWeb.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace ControlGastosWeb.Controllers
{
   [Authorize]
   public class PresupuestoController : Controller
   {
       private readonly ApplicationDbContext _context;

       public PresupuestoController(ApplicationDbContext context)
       {
           _context = context;
       }

       public async Task<IActionResult> Index()
       {
           var presupuestos = await _context.Presupuestos
               .Include(p => p.TipoGasto)
               .OrderByDescending(p => p.Ano)
               .ThenByDescending(p => p.Mes)
               .ToListAsync();

           var totalPresupuestos = presupuestos.Count;
           var totalPresupuestado = presupuestos.Sum(p => p.MontoPresupuestado);

           ViewBag.TotalPresupuestos = totalPresupuestos;
           ViewBag.TotalPresupuestado = totalPresupuestado;

           return View(presupuestos);
       }

       public async Task<IActionResult> Create()
       {
           ViewData["TipoGastoId"] = new SelectList(await _context.TiposGasto.ToListAsync(), "Id", "Nombre");
           return View();
       }

       [HttpPost]
       [ValidateAntiForgeryToken]
       public async Task<IActionResult> Create([Bind("Id,Mes,Ano,TipoGastoId,MontoPresupuestado")] Presupuesto presupuesto)
       {
           if (ModelState.IsValid)
           {
               var existePresupuesto = await _context.Presupuestos
                   .AnyAsync(p => p.TipoGastoId == presupuesto.TipoGastoId &&
                                 p.Ano == presupuesto.Ano &&
                                 p.Mes == presupuesto.Mes);

               if (existePresupuesto)
               {
                   TempData["ErrorMessage"] = "Ya existe un presupuesto para este tipo de gasto en el período seleccionado.";
                   ViewData["TipoGastoId"] = new SelectList(await _context.TiposGasto.ToListAsync(), "Id", "Nombre", presupuesto.TipoGastoId);
                   return View(presupuesto);
               }

               presupuesto.FechaCreacion = DateTime.Now;
               presupuesto.Activo = true;
               presupuesto.MontoEjecutado = 0;

               _context.Add(presupuesto);
               await _context.SaveChangesAsync();
               TempData["SuccessMessage"] = "Presupuesto creado exitosamente.";
               return RedirectToAction(nameof(Index));
           }

           ViewData["TipoGastoId"] = new SelectList(await _context.TiposGasto.ToListAsync(), "Id", "Nombre", presupuesto.TipoGastoId);
           return View(presupuesto);
       }

       public async Task<IActionResult> Edit(int? id)
       {
           if (id == null) return NotFound();

           var presupuesto = await _context.Presupuestos.FindAsync(id);
           if (presupuesto == null) return NotFound();

           ViewData["TipoGastoId"] = new SelectList(await _context.TiposGasto.ToListAsync(), "Id", "Nombre", presupuesto.TipoGastoId);
           return View(presupuesto);
       }

       [HttpPost]
       [ValidateAntiForgeryToken]
       public async Task<IActionResult> Edit(int id, [Bind("Id,Mes,Ano,TipoGastoId,MontoPresupuestado,FechaCreacion,Activo")] Presupuesto presupuesto)
       {
           if (id != presupuesto.Id) return NotFound();

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
                       return NotFound();
                   else
                       throw;
               }
               return RedirectToAction(nameof(Index));
           }

           ViewData["TipoGastoId"] = new SelectList(await _context.TiposGasto.ToListAsync(), "Id", "Nombre", presupuesto.TipoGastoId);
           return View(presupuesto);
       }

       public async Task<IActionResult> Delete(int? id)
       {
           if (id == null) return NotFound();

           var presupuesto = await _context.Presupuestos
               .Include(p => p.TipoGasto)
               .FirstOrDefaultAsync(m => m.Id == id);

           if (presupuesto == null) return NotFound();
           return View(presupuesto);
       }

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

       public IActionResult Reporte()
       {
           return View();
       }

       [HttpGet]
       public async Task<IActionResult> ObtenerDatosGrafico(DateTime? fechaInicio, DateTime? fechaFin)
       {
           try
           {
               if (!fechaInicio.HasValue || !fechaFin.HasValue)
               {
                   var now = DateTime.Now;
                   fechaInicio = new DateTime(now.Year, now.Month, 1);
                   fechaFin = fechaInicio.Value.AddMonths(1).AddDays(-1);
               }

               var anoInicio = fechaInicio.Value.Year;
               var mesInicio = fechaInicio.Value.Month;
               var anoFin = fechaFin.Value.Year;
               var mesFin = fechaFin.Value.Month;

               var presupuestos = await _context.Presupuestos
                   .Include(p => p.TipoGasto)
                   .Where(p => (p.Ano > anoInicio || (p.Ano == anoInicio && p.Mes >= mesInicio)) &&
                              (p.Ano < anoFin || (p.Ano == anoFin && p.Mes <= mesFin)))
                   .ToListAsync();

               if (presupuestos.Count == 0)
               {
                   return Json(new { 
                       success = true, 
                       datos = new List<object>(),
                       totalPresupuestado = 0,
                       totalEjecutado = 0
                   });
               }

               var gastos = await _context.Gastos
                   .Include(g => g.TipoGasto)
                   .Where(g => g.FechaGasto >= fechaInicio.Value && g.FechaGasto <= fechaFin.Value)
                   .ToListAsync();

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
               return Json(new { success = false, message = "Error: " + ex.Message });
           }
       }

       private bool PresupuestoExists(int id)
       {
           return _context.Presupuestos.Any(e => e.Id == id);
       }
   }
}

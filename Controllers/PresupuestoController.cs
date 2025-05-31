using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IActionResult> Index()
        {
            var presupuestos = await _context.Presupuestos
                .Include(p => p.TipoGasto)
                .OrderByDescending(p => p.Ano)
                .ThenByDescending(p => p.Mes)
                .ToListAsync();
            return View(presupuestos);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["TipoGastoId"] = new SelectList(await _context.TiposGasto.ToListAsync(), "Id", "Nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Presupuesto presupuesto)
        {
            if (ModelState.IsValid)
            {
                presupuesto.FechaCreacion = DateTime.Now;
                presupuesto.Activo = true;
                presupuesto.MontoEjecutado = 0;

                _context.Add(presupuesto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["TipoGastoId"] = new SelectList(await _context.TiposGasto.ToListAsync(), "Id", "Nombre");
            return View(presupuesto);
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
                var datos = new List<object>();
                return Json(new { 
                    success = true, 
                    datos = datos,
                    totalPresupuestado = 0,
                    totalEjecutado = 0
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}

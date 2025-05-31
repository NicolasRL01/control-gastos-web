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
            var presupuestos = await _context.Presupuestos.Include(p => p.TipoGasto).ToListAsync();
            return View(presupuestos);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["TipoGastoId"] = new SelectList(await _context.TiposGasto.ToListAsync(), "Id", "Nombre");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Presupuesto presupuesto)
        {
            presupuesto.FechaCreacion = DateTime.Now;
            presupuesto.Activo = true;
            presupuesto.MontoEjecutado = 0;
            _context.Add(presupuesto);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public IActionResult Reporte()
        {
            return View();
        }
    }
}

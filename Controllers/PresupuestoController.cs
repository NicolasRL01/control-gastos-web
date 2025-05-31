using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlGastosWeb.Models;

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
            var lista = await _context.Presupuestos.ToListAsync();
            return View(lista);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Presupuesto modelo)
        {
            modelo.FechaCreacion = DateTime.Now;
            modelo.Activo = true;
            _context.Add(modelo);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public IActionResult Reporte()
        {
            return View();
        }
    }
}

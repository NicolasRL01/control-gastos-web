using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ControlGastosWeb.Models;

namespace ControlGastosWeb.Controllers
{
    [Authorize]
    public class FondoMonetarioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FondoMonetarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: FondoMonetario
        public async Task<IActionResult> Index()
        {
            return View(await _context.FondosMonetarios.Where(f => f.Activo).ToListAsync());
        }

        // GET: FondoMonetario/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var fondo = await _context.FondosMonetarios.FirstOrDefaultAsync(m => m.Id == id);
            if (fondo == null) return NotFound();
            return View(fondo);
        }

        // GET: FondoMonetario/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: FondoMonetario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Tipo,SaldoInicial")] FondoMonetario fondo)
        {
            if (ModelState.IsValid)
            {
                fondo.FechaCreacion = DateTime.Now;
                fondo.SaldoActual = fondo.SaldoInicial;
                fondo.Activo = true;
                _context.Add(fondo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(fondo);
        }

        // GET: FondoMonetario/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var fondo = await _context.FondosMonetarios.FindAsync(id);
            if (fondo == null) return NotFound();
            return View(fondo);
        }

        // POST: FondoMonetario/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Tipo,SaldoInicial,SaldoActual,FechaCreacion,Activo")] FondoMonetario fondo)
        {
            if (id != fondo.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(fondo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FondoExists(fondo.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(fondo);
        }

        // GET: FondoMonetario/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var fondo = await _context.FondosMonetarios.FirstOrDefaultAsync(m => m.Id == id);
            if (fondo == null) return NotFound();
            return View(fondo);
        }

        // POST: FondoMonetario/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fondo = await _context.FondosMonetarios.FindAsync(id);
            if (fondo != null)
            {
                fondo.Activo = false;
                _context.Update(fondo);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool FondoExists(int id)
        {
            return _context.FondosMonetarios.Any(e => e.Id == id);
        }
    }
}
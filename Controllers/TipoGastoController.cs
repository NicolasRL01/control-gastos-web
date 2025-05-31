using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ControlGastosWeb.Models;

namespace ControlGastosWeb.Controllers
{
    [Authorize]
    public class TipoGastoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TipoGastoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TipoGasto
        public async Task<IActionResult> Index()
        {
            return View(await _context.TiposGasto.Where(t => t.Activo).ToListAsync());
        }

        // GET: TipoGasto/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var tipoGasto = await _context.TiposGasto.FirstOrDefaultAsync(m => m.Id == id);
            if (tipoGasto == null) return NotFound();
            return View(tipoGasto);
        }

        // GET: TipoGasto/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TipoGasto/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Codigo,Nombre")] TipoGasto tipoGasto)
        {
            // **GENERACIÓN AUTOMÁTICA DE CÓDIGO ANTES DE VALIDAR**
            // Buscar el último código que empiece con "TG"
            var ultimoCodigo = await _context.TiposGasto
                .Where(t => t.Codigo.StartsWith("TG") && t.Activo)
                .OrderByDescending(t => t.Codigo)
                .Select(t => t.Codigo)
                .FirstOrDefaultAsync();

            // Generar el siguiente código secuencial
            if (ultimoCodigo == null)
            {
                // Es el primer tipo de gasto
                tipoGasto.Codigo = "TG001";
            }
            else
            {
                // Extraer el número del último código (ej: TG003 -> 3)
                string numeroStr = ultimoCodigo.Substring(2); // Quita "TG"
                int numero = int.Parse(numeroStr) + 1;
                tipoGasto.Codigo = $"TG{numero:D3}"; // Formato TG001, TG002, etc.
            }

            // Limpiar el error de validación del código
            ModelState.Remove("Codigo");

            if (ModelState.IsValid)
            {
                // Asignar valores automáticos
                tipoGasto.FechaCreacion = DateTime.Now;
                tipoGasto.Activo = true;

                _context.Add(tipoGasto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tipoGasto);
        }

        // GET: TipoGasto/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var tipoGasto = await _context.TiposGasto.FindAsync(id);
            if (tipoGasto == null) return NotFound();
            return View(tipoGasto);
        }

        // POST: TipoGasto/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Codigo,Nombre,FechaCreacion,Activo")] TipoGasto tipoGasto)
        {
            if (id != tipoGasto.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tipoGasto);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TipoGastoExists(tipoGasto.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tipoGasto);
        }

        // GET: TipoGasto/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var tipoGasto = await _context.TiposGasto.FirstOrDefaultAsync(m => m.Id == id);
            if (tipoGasto == null) return NotFound();
            return View(tipoGasto);
        }

        // POST: TipoGasto/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tipoGasto = await _context.TiposGasto.FindAsync(id);
            if (tipoGasto != null)
            {
                tipoGasto.Activo = false; // Eliminación lógica
                _context.Update(tipoGasto);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool TipoGastoExists(int id)
        {
            return _context.TiposGasto.Any(e => e.Id == id);
        }
    }
}
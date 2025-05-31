using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ControlGastosWeb.Models;

namespace ControlGastosWeb.Controllers
{
    [Authorize]
    public class DepositoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepositoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Deposito
        public async Task<IActionResult> Index()
        {
            var depositos = _context.Depositos
                .Include(d => d.FondoMonetario)
                .Where(d => d.Activo)
                .OrderByDescending(d => d.Fecha);
            return View(await depositos.ToListAsync());
        }

        // GET: Deposito/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var deposito = await _context.Depositos
                .Include(d => d.FondoMonetario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (deposito == null) return NotFound();

            return View(deposito);
        }

        // GET: Deposito/Create
        public async Task<IActionResult> Create()
        {
            await CargarFondosMonetarios();

            // Inicializar con fecha de hoy por defecto
            var deposito = new Deposito
            {
                Fecha = DateTime.Today
            };

            return View(deposito);
        }

        // POST: Deposito/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Fecha,FondoMonetarioId,Monto,Descripcion")] Deposito deposito)
        {
            if (ModelState.IsValid)
            {
                deposito.FechaCreacion = DateTime.Now;
                deposito.Activo = true;
                _context.Add(deposito);

                // Actualizar saldo del fondo
                var fondo = await _context.FondosMonetarios.FindAsync(deposito.FondoMonetarioId);
                if (fondo != null)
                {
                    fondo.SaldoActual += deposito.Monto;
                    _context.Update(fondo);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Si hay errores, recargar el ViewBag
            await CargarFondosMonetarios(deposito.FondoMonetarioId);
            return View(deposito);
        }

        // GET: Deposito/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var deposito = await _context.Depositos.FindAsync(id);
            if (deposito == null) return NotFound();

            await CargarFondosMonetarios(deposito.FondoMonetarioId);
            return View(deposito);
        }

        // POST: Deposito/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Fecha,FondoMonetarioId,Monto,Descripcion,FechaCreacion,Activo")] Deposito deposito)
        {
            if (id != deposito.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Obtener el depósito original para calcular la diferencia en el saldo
                    var depositoOriginal = await _context.Depositos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
                    if (depositoOriginal != null)
                    {
                        // Revertir el saldo del fondo original
                        var fondoOriginal = await _context.FondosMonetarios.FindAsync(depositoOriginal.FondoMonetarioId);
                        if (fondoOriginal != null)
                        {
                            fondoOriginal.SaldoActual -= depositoOriginal.Monto;
                            _context.Update(fondoOriginal);
                        }

                        // Aplicar el nuevo saldo al fondo seleccionado
                        var fondoNuevo = await _context.FondosMonetarios.FindAsync(deposito.FondoMonetarioId);
                        if (fondoNuevo != null)
                        {
                            fondoNuevo.SaldoActual += deposito.Monto;
                            _context.Update(fondoNuevo);
                        }
                    }

                    _context.Update(deposito);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepositoExists(deposito.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await CargarFondosMonetarios(deposito.FondoMonetarioId);
            return View(deposito);
        }

        // GET: Deposito/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var deposito = await _context.Depositos
                .Include(d => d.FondoMonetario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (deposito == null) return NotFound();

            return View(deposito);
        }

        // POST: Deposito/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deposito = await _context.Depositos.FindAsync(id);
            if (deposito != null)
            {
                deposito.Activo = false;
                _context.Update(deposito);

                // Revertir saldo del fondo
                var fondo = await _context.FondosMonetarios.FindAsync(deposito.FondoMonetarioId);
                if (fondo != null)
                {
                    fondo.SaldoActual -= deposito.Monto;
                    _context.Update(fondo);
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DepositoExists(int id)
        {
            return _context.Depositos.Any(e => e.Id == id);
        }

        // Método helper para cargar fondos monetarios
        private async Task CargarFondosMonetarios(int? fondoSeleccionado = null)
        {
            var fondos = await _context.FondosMonetarios
                .Where(f => f.Activo)
                .OrderBy(f => f.Nombre)
                .ToListAsync();

            ViewBag.FondoMonetarioId = new SelectList(fondos, "Id", "Nombre", fondoSeleccionado);
        }
    }
}
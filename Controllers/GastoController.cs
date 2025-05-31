using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ControlGastosWeb.Models;
using Microsoft.AspNetCore.Authorization;

namespace ControlGastosWeb.Controllers
{
    [Authorize]
    public class GastoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GastoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Gasto
        public async Task<IActionResult> Index()
        {
            var gastos = _context.Gastos
                .Include(g => g.FondoMonetario)
                .Include(g => g.TipoGasto)
                .Where(g => g.Activo)
                .OrderByDescending(g => g.FechaGasto);

            return View(await gastos.ToListAsync());
        }

        // GET: Gasto/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gasto = await _context.Gastos
                .Include(g => g.FondoMonetario)
                .Include(g => g.TipoGasto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (gasto == null)
            {
                return NotFound();
            }

            return View(gasto);
        }

        // GET: Gasto/Create
        public IActionResult Create()
        {
            Console.WriteLine("=== GET CREATE GASTO ===");

            try
            {
                // Cargar datos para los dropdowns
                var tiposGasto = _context.TiposGasto.Where(t => t.Activo).ToList();
                var fondosMonetarios = _context.FondosMonetarios.Where(f => f.Activo).ToList();

                Console.WriteLine($"Tipos de Gasto encontrados: {tiposGasto.Count}");
                Console.WriteLine($"Fondos Monetarios encontrados: {fondosMonetarios.Count}");

                ViewData["TipoGastoId"] = new SelectList(tiposGasto, "Id", "Nombre");
                ViewData["FondoMonetarioId"] = new SelectList(fondosMonetarios, "Id", "Nombre");

                // Lista de tipos de documento
                var tiposDocumento = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Factura", Text = "Factura" },
                    new SelectListItem { Value = "Recibo", Text = "Recibo" },
                    new SelectListItem { Value = "Ticket", Text = "Ticket" },
                    new SelectListItem { Value = "Voucher", Text = "Voucher" },
                    new SelectListItem { Value = "Otro", Text = "Otro" }
                };
                ViewBag.TiposDocumento = new SelectList(tiposDocumento, "Value", "Text");

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GET Create: {ex.Message}");
                return View("Error");
            }
        }

        // POST: Gasto/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Descripcion,Monto,FechaGasto,TipoGastoId,Observaciones,TipoDocumento,NumeroDocumento,FondoMonetarioId,NombreComercio")] Gasto gasto)
        {
            Console.WriteLine("=== CREATE GASTO POST ===");
            Console.WriteLine($"TipoGasto: {gasto.TipoGastoId}");
            Console.WriteLine($"Fecha: {gasto.FechaGasto}");
            Console.WriteLine($"Monto: {gasto.Monto}");
            Console.WriteLine($"Fondo: {gasto.FondoMonetarioId}");
            Console.WriteLine($"Comercio: {gasto.NombreComercio}");
            Console.WriteLine($"TipoDoc: {gasto.TipoDocumento}");
            Console.WriteLine($"NumeroDoc: {gasto.NumeroDocumento}");
            Console.WriteLine($"Observaciones: {gasto.Observaciones}");

            // LIMPIAR ERRORES DE NAVEGACIÓN
            ModelState.Remove("TipoGasto");
            ModelState.Remove("FondoMonetario");

            // VALIDAR QUE HAY FONDOS SUFICIENTES
            var fondo = await _context.FondosMonetarios.FindAsync(gasto.FondoMonetarioId);
            if (fondo != null && fondo.SaldoActual < gasto.Monto)
            {
                ModelState.AddModelError("Monto", $"Fondos insuficientes. Saldo disponible: ${fondo.SaldoActual:N2}");
                Console.WriteLine($"FONDOS INSUFICIENTES: Disponible ${fondo.SaldoActual}, Requerido ${gasto.Monto}");
            }

            Console.WriteLine("=== ERRORES EN MODELSTATE ===");
            foreach (var error in ModelState)
            {
                Console.WriteLine($"Campo: {error.Key}");
                if (error.Value.Errors.Any())
                {
                    foreach (var err in error.Value.Errors)
                    {
                        Console.WriteLine($"  Error: {err.ErrorMessage}");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Establecer fecha de creación
                    gasto.FechaCreacion = DateTime.Now;
                    gasto.Activo = true;

                    // ACTUALIZAR SALDO DEL FONDO
                    if (fondo != null)
                    {
                        Console.WriteLine($"ACTUALIZANDO FONDO: {fondo.Nombre}");
                        Console.WriteLine($"Saldo antes: ${fondo.SaldoActual}");

                        fondo.SaldoActual -= gasto.Monto;
                        _context.Update(fondo);

                        Console.WriteLine($"Saldo después: ${fondo.SaldoActual}");
                    }

                    // ACTUALIZAR PRESUPUESTO
                    await ActualizarPresupuestoAlCrearGasto(gasto);

                    _context.Add(gasto);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("GASTO GUARDADO EXITOSAMENTE, FONDO Y PRESUPUESTO ACTUALIZADOS");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR AL GUARDAR: {ex.Message}");
                    ModelState.AddModelError("", "Error al guardar el gasto: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("MODELSTATE NO ES VÁLIDO");
            }

            // Si llegamos aquí, algo salió mal, volver a mostrar el formulario
            ViewData["TipoGastoId"] = new SelectList(_context.TiposGasto.Where(t => t.Activo), "Id", "Nombre", gasto.TipoGastoId);
            ViewData["FondoMonetarioId"] = new SelectList(_context.FondosMonetarios.Where(f => f.Activo), "Id", "Nombre", gasto.FondoMonetarioId);

            // Lista de tipos de documento
            var tiposDocumento = new List<SelectListItem>
            {
                new SelectListItem { Value = "Factura", Text = "Factura" },
                new SelectListItem { Value = "Recibo", Text = "Recibo" },
                new SelectListItem { Value = "Ticket", Text = "Ticket" },
                new SelectListItem { Value = "Voucher", Text = "Voucher" },
                new SelectListItem { Value = "Otro", Text = "Otro" }
            };
            ViewBag.TiposDocumento = new SelectList(tiposDocumento, "Value", "Text", gasto.TipoDocumento);

            return View(gasto);
        }

        // GET: Gasto/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gasto = await _context.Gastos.FindAsync(id);
            if (gasto == null)
            {
                return NotFound();
            }

            ViewData["TipoGastoId"] = new SelectList(_context.TiposGasto.Where(t => t.Activo), "Id", "Nombre", gasto.TipoGastoId);
            ViewData["FondoMonetarioId"] = new SelectList(_context.FondosMonetarios.Where(f => f.Activo), "Id", "Nombre", gasto.FondoMonetarioId);

            // Lista de tipos de documento
            var tiposDocumento = new List<SelectListItem>
            {
                new SelectListItem { Value = "Factura", Text = "Factura" },
                new SelectListItem { Value = "Recibo", Text = "Recibo" },
                new SelectListItem { Value = "Ticket", Text = "Ticket" },
                new SelectListItem { Value = "Voucher", Text = "Voucher" },
                new SelectListItem { Value = "Otro", Text = "Otro" }
            };
            ViewBag.TiposDocumento = new SelectList(tiposDocumento, "Value", "Text", gasto.TipoDocumento);

            return View(gasto);
        }

        // POST: Gasto/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Descripcion,Monto,FechaGasto,TipoGastoId,Observaciones,TipoDocumento,NumeroDocumento,FondoMonetarioId,NombreComercio,FechaCreacion,Activo")] Gasto gasto)
        {
            if (id != gasto.Id)
            {
                return NotFound();
            }

            // LIMPIAR ERRORES DE NAVEGACIÓN
            ModelState.Remove("TipoGasto");
            ModelState.Remove("FondoMonetario");

            if (ModelState.IsValid)
            {
                try
                {
                    Console.WriteLine("=== EDITANDO GASTO ===");

                    // Obtener el gasto original para revertir los cambios anteriores
                    var gastoOriginal = await _context.Gastos.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);

                    if (gastoOriginal != null)
                    {
                        Console.WriteLine($"Gasto Original: {gastoOriginal.Descripcion}, Monto: ${gastoOriginal.Monto}, TipoGasto: {gastoOriginal.TipoGastoId}, Fondo: {gastoOriginal.FondoMonetarioId}");
                        Console.WriteLine($"Gasto Nuevo: {gasto.Descripcion}, Monto: ${gasto.Monto}, TipoGasto: {gasto.TipoGastoId}, Fondo: {gasto.FondoMonetarioId}");

                        // 1. REVERTIR EL GASTO ANTERIOR EN EL FONDO ORIGINAL
                        var fondoOriginal = await _context.FondosMonetarios.FindAsync(gastoOriginal.FondoMonetarioId);
                        if (fondoOriginal != null)
                        {
                            Console.WriteLine($"Revirtiendo ${gastoOriginal.Monto} al fondo {fondoOriginal.Nombre}");
                            fondoOriginal.SaldoActual += gastoOriginal.Monto;
                            _context.Update(fondoOriginal);
                        }

                        // 2. APLICAR EL NUEVO GASTO AL FONDO NUEVO
                        var fondoNuevo = await _context.FondosMonetarios.FindAsync(gasto.FondoMonetarioId);
                        if (fondoNuevo != null)
                        {
                            Console.WriteLine($"Aplicando ${gasto.Monto} al fondo {fondoNuevo.Nombre}");
                            fondoNuevo.SaldoActual -= gasto.Monto;
                            _context.Update(fondoNuevo);
                        }

                        // 3. ACTUALIZAR PRESUPUESTOS
                        await ActualizarPresupuestosAlEditarGasto(gastoOriginal, gasto);
                    }

                    _context.Update(gasto);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("Gasto editado exitosamente con fondos y presupuestos actualizados");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GastoExists(gasto.Id))
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

            ViewData["TipoGastoId"] = new SelectList(_context.TiposGasto.Where(t => t.Activo), "Id", "Nombre", gasto.TipoGastoId);
            ViewData["FondoMonetarioId"] = new SelectList(_context.FondosMonetarios.Where(f => f.Activo), "Id", "Nombre", gasto.FondoMonetarioId);

            // Lista de tipos de documento
            var tiposDocumento = new List<SelectListItem>
            {
                new SelectListItem { Value = "Factura", Text = "Factura" },
                new SelectListItem { Value = "Recibo", Text = "Recibo" },
                new SelectListItem { Value = "Ticket", Text = "Ticket" },
                new SelectListItem { Value = "Voucher", Text = "Voucher" },
                new SelectListItem { Value = "Otro", Text = "Otro" }
            };
            ViewBag.TiposDocumento = new SelectList(tiposDocumento, "Value", "Text", gasto.TipoDocumento);

            return View(gasto);
        }

        // GET: Gasto/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gasto = await _context.Gastos
                .Include(g => g.FondoMonetario)
                .Include(g => g.TipoGasto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (gasto == null)
            {
                return NotFound();
            }

            return View(gasto);
        }

        // POST: Gasto/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var gasto = await _context.Gastos
                .Include(g => g.TipoGasto)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gasto != null)
            {
                Console.WriteLine($"=== ELIMINANDO GASTO ===");
                Console.WriteLine($"Gasto: {gasto.Descripcion}, Monto: ${gasto.Monto}, TipoGasto: {gasto.TipoGasto?.Nombre}");
                Console.WriteLine($"IMPORTANTE: NO se regresa dinero al fondo - Es un gasto ya pagado");

                try
                {
                    // ACTUALIZAR PRESUPUESTO - Revertir el gasto del presupuesto
                    await ActualizarPresupuestoAlEliminarGasto(gasto);

                    // Soft delete - solo marcar como inactivo
                    // NO devolvemos dinero al fondo porque es un gasto ya pagado
                    gasto.Activo = false;
                    _context.Update(gasto);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("Gasto eliminado exitosamente - Presupuesto actualizado, fondo sin cambios");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al eliminar gasto: {ex.Message}");
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool GastoExists(int id)
        {
            return _context.Gastos.Any(e => e.Id == id);
        }

        // MÉTODO AUXILIAR PARA ACTUALIZAR PRESUPUESTO AL CREAR
        private async Task ActualizarPresupuestoAlCrearGasto(Gasto gasto)
        {
            Console.WriteLine("=== ACTUALIZANDO PRESUPUESTO AL CREAR ===");

            try
            {
                // Buscar el presupuesto correspondiente usando Mes y Ano
                var presupuesto = await _context.Presupuestos
                    .FirstOrDefaultAsync(p => p.TipoGastoId == gasto.TipoGastoId &&
                                             p.Mes == gasto.FechaGasto.Month &&
                                             p.Ano == gasto.FechaGasto.Year &&
                                             p.Activo);

                if (presupuesto != null)
                {
                    Console.WriteLine($"Presupuesto encontrado: {presupuesto.Id}");
                    Console.WriteLine($"MontoEjecutado antes: ${presupuesto.MontoEjecutado}");
                    Console.WriteLine($"Sumando ${gasto.Monto} al presupuesto");

                    presupuesto.MontoEjecutado += gasto.Monto;
                    _context.Update(presupuesto);

                    Console.WriteLine($"MontoEjecutado después: ${presupuesto.MontoEjecutado}");
                }
                else
                {
                    Console.WriteLine($"No se encontró presupuesto para TipoGasto: {gasto.TipoGastoId}, Período: {gasto.FechaGasto:MM/yyyy}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando presupuesto al crear: {ex.Message}");
            }
        }

        // MÉTODO AUXILIAR PARA ACTUALIZAR PRESUPUESTOS AL EDITAR
        private async Task ActualizarPresupuestosAlEditarGasto(Gasto gastoOriginal, Gasto gastoNuevo)
        {
            Console.WriteLine("=== ACTUALIZANDO PRESUPUESTOS AL EDITAR ===");

            try
            {
                // Obtener información del tipo de gasto original y nuevo
                var tipoGastoOriginal = await _context.TiposGasto.FindAsync(gastoOriginal.TipoGastoId);
                var tipoGastoNuevo = await _context.TiposGasto.FindAsync(gastoNuevo.TipoGastoId);

                if (tipoGastoOriginal != null && tipoGastoNuevo != null)
                {
                    // 1. REVERTIR EL GASTO ORIGINAL DEL PRESUPUESTO ANTERIOR
                    var presupuestoOriginal = await _context.Presupuestos
                        .FirstOrDefaultAsync(p => p.TipoGastoId == gastoOriginal.TipoGastoId &&
                                                 p.Mes == gastoOriginal.FechaGasto.Month &&
                                                 p.Ano == gastoOriginal.FechaGasto.Year &&
                                                 p.Activo);

                    if (presupuestoOriginal != null)
                    {
                        Console.WriteLine($"Revirtiendo ${gastoOriginal.Monto} del presupuesto {tipoGastoOriginal.Nombre}");
                        Console.WriteLine($"MontoEjecutado antes: ${presupuestoOriginal.MontoEjecutado}");

                        presupuestoOriginal.MontoEjecutado -= gastoOriginal.Monto;

                        // Asegurar que no sea negativo
                        if (presupuestoOriginal.MontoEjecutado < 0)
                        {
                            presupuestoOriginal.MontoEjecutado = 0;
                        }

                        _context.Update(presupuestoOriginal);
                        Console.WriteLine($"MontoEjecutado después: ${presupuestoOriginal.MontoEjecutado}");
                    }

                    // 2. APLICAR EL NUEVO GASTO AL PRESUPUESTO NUEVO
                    var presupuestoNuevo = await _context.Presupuestos
                        .FirstOrDefaultAsync(p => p.TipoGastoId == gastoNuevo.TipoGastoId &&
                                                 p.Mes == gastoNuevo.FechaGasto.Month &&
                                                 p.Ano == gastoNuevo.FechaGasto.Year &&
                                                 p.Activo);

                    if (presupuestoNuevo != null)
                    {
                        Console.WriteLine($"Aplicando ${gastoNuevo.Monto} al presupuesto {tipoGastoNuevo.Nombre}");
                        Console.WriteLine($"MontoEjecutado antes: ${presupuestoNuevo.MontoEjecutado}");

                        presupuestoNuevo.MontoEjecutado += gastoNuevo.Monto;
                        _context.Update(presupuestoNuevo);

                        Console.WriteLine($"MontoEjecutado después: ${presupuestoNuevo.MontoEjecutado}");
                    }
                    else
                    {
                        Console.WriteLine($"No se encontró presupuesto para {tipoGastoNuevo.Nombre} en {gastoNuevo.FechaGasto:MM/yyyy}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando presupuestos: {ex.Message}");
            }
        }

        // MÉTODO AUXILIAR PARA ACTUALIZAR PRESUPUESTO AL ELIMINAR
        private async Task ActualizarPresupuestoAlEliminarGasto(Gasto gasto)
        {
            Console.WriteLine("=== ACTUALIZANDO PRESUPUESTO AL ELIMINAR ===");

            try
            {
                if (gasto.TipoGasto != null)
                {
                    // Buscar el presupuesto correspondiente usando Mes y Ano
                    var presupuesto = await _context.Presupuestos
                        .FirstOrDefaultAsync(p => p.TipoGastoId == gasto.TipoGastoId &&
                                                 p.Mes == gasto.FechaGasto.Month &&
                                                 p.Ano == gasto.FechaGasto.Year &&
                                                 p.Activo);

                    if (presupuesto != null)
                    {
                        Console.WriteLine($"Revirtiendo ${gasto.Monto} del presupuesto {gasto.TipoGasto.Nombre}");
                        Console.WriteLine($"MontoEjecutado antes: ${presupuesto.MontoEjecutado}");

                        presupuesto.MontoEjecutado -= gasto.Monto;

                        // Asegurar que el ejecutado no sea negativo
                        if (presupuesto.MontoEjecutado < 0)
                        {
                            presupuesto.MontoEjecutado = 0;
                        }

                        Console.WriteLine($"MontoEjecutado después: ${presupuesto.MontoEjecutado}");
                        _context.Update(presupuesto);
                    }
                    else
                    {
                        Console.WriteLine($"No se encontró presupuesto para {gasto.TipoGasto.Nombre} en {gasto.FechaGasto:MM/yyyy}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando presupuesto al eliminar: {ex.Message}");
            }
        }
    }
}
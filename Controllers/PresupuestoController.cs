using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ControlGastosWeb.Models;

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

        // ===== MÉTODOS DE INTEGRACIÓN GASTOS ↔ PRESUPUESTOS =====
        private async Task ActualizarMontoEjecutadoPresupuesto(int tipoGastoId, int mes, int ano)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== ACTUALIZANDO MONTO EJECUTADO - TipoGasto: {tipoGastoId}, Mes: {mes}, Año: {ano} ===");

                var presupuesto = await _context.Presupuestos
                    .FirstOrDefaultAsync(p => p.TipoGastoId == tipoGastoId
                                           && p.Mes == mes
                                           && p.Ano == ano
                                           && p.Activo);

                if (presupuesto != null)
                {
                    // FIX SQLite: Usar ToListAsync y luego Sum en C#
                    var gastosDelPeriodo = await _context.Gastos
                        .Where(g => g.TipoGastoId == tipoGastoId
                                   && g.FechaGasto.Month == mes
                                   && g.FechaGasto.Year == ano
                                   && g.Activo)
                        .ToListAsync();

                    var totalGastado = gastosDelPeriodo.Sum(g => g.Monto);

                    System.Diagnostics.Debug.WriteLine($"Total gastado calculado: {totalGastado}");
                    System.Diagnostics.Debug.WriteLine($"Monto ejecutado anterior: {presupuesto.MontoEjecutado}");

                    presupuesto.MontoEjecutado = totalGastado;
                    _context.Update(presupuesto);
                    await _context.SaveChangesAsync();

                    System.Diagnostics.Debug.WriteLine($"Presupuesto actualizado - Nuevo monto ejecutado: {presupuesto.MontoEjecutado}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No se encontró presupuesto activo para TipoGasto: {tipoGastoId}, Mes: {mes}, Año: {ano}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR al actualizar monto ejecutado: {ex.Message}");
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> RecalcularTodosLosPresupuestos()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== RECALCULANDO TODOS LOS PRESUPUESTOS ===");

                var presupuestosActivos = await _context.Presupuestos
                    .Where(p => p.Activo)
                    .ToListAsync();

                var resultados = new List<object>();

                foreach (var presupuesto in presupuestosActivos)
                {
                    // FIX SQLite: Usar ToListAsync y luego Sum en C#
                    var gastosDelPresupuesto = await _context.Gastos
                        .Where(g => g.TipoGastoId == presupuesto.TipoGastoId
                                   && g.FechaGasto.Month == presupuesto.Mes
                                   && g.FechaGasto.Year == presupuesto.Ano
                                   && g.Activo)
                        .ToListAsync();

                    var totalGastado = gastosDelPresupuesto.Sum(g => g.Monto);

                    var montoAnterior = presupuesto.MontoEjecutado;
                    presupuesto.MontoEjecutado = totalGastado;

                    resultados.Add(new
                    {
                        PresupuestoId = presupuesto.Id,
                        TipoGastoId = presupuesto.TipoGastoId,
                        Mes = presupuesto.Mes,
                        Ano = presupuesto.Ano,
                        MontoAnterior = montoAnterior,
                        MontoNuevo = totalGastado,
                        Diferencia = totalGastado - montoAnterior
                    });

                    System.Diagnostics.Debug.WriteLine($"Presupuesto ID {presupuesto.Id}: {montoAnterior} → {totalGastado}");
                }

                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"Recálculo completado para {presupuestosActivos.Count} presupuestos");

                return Json(new
                {
                    success = true,
                    mensaje = $"Se recalcularon {presupuestosActivos.Count} presupuestos",
                    resultados = resultados
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR en recálculo: {ex.Message}");
                return Json(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        // ===== ENDPOINTS DE LIMPIEZA Y DEBUG =====
        [AllowAnonymous]
        public async Task<IActionResult> LimpiarTodosInactivos()
        {
            try
            {
                var registrosInactivos = await _context.Presupuestos
                    .Where(p => !p.Activo)
                    .ToListAsync();

                if (registrosInactivos.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Eliminando {registrosInactivos.Count} registros inactivos");
                    _context.Presupuestos.RemoveRange(registrosInactivos);
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        mensaje = $"Se eliminaron {registrosInactivos.Count} registros inactivos",
                        registrosEliminados = registrosInactivos.Select(r => new {
                            r.Id,
                            r.TipoGastoId,
                            r.Mes,
                            r.Ano
                        })
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = true,
                        mensaje = "No se encontraron registros inactivos para eliminar"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR AL LIMPIAR INACTIVOS: {ex.Message}");
                return Json(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> VerificarYArreglarIndice()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== ARREGLANDO ÍNDICE DEFINITIVAMENTE ===");

                await _context.Database.ExecuteSqlRawAsync(@"
                    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Presupuestos_TipoGastoId_Mes_Ano' AND object_id = OBJECT_ID('Presupuestos'))
                    BEGIN
                        DROP INDEX IX_Presupuestos_TipoGastoId_Mes_Ano ON Presupuestos;
                        PRINT 'Índice IX_Presupuestos_TipoGastoId_Mes_Ano eliminado';
                    END

                    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Presupuestos_TipoGastoId_Mes_Ano_Activo' AND object_id = OBJECT_ID('Presupuestos'))
                    BEGIN
                        DROP INDEX IX_Presupuestos_TipoGastoId_Mes_Ano_Activo ON Presupuestos;
                        PRINT 'Índice IX_Presupuestos_TipoGastoId_Mes_Ano_Activo eliminado';
                    END

                    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Presupuestos_TipoGastoId_Mes_Ano_SoloActivos' AND object_id = OBJECT_ID('Presupuestos'))
                    BEGIN
                        DROP INDEX IX_Presupuestos_TipoGastoId_Mes_Ano_SoloActivos ON Presupuestos;
                        PRINT 'Índice IX_Presupuestos_TipoGastoId_Mes_Ano_SoloActivos eliminado';
                    END

                    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Presupuestos_TipoGastoId_Mes_Ano_Activos' AND object_id = OBJECT_ID('Presupuestos'))
                    BEGIN
                        DROP INDEX IX_Presupuestos_TipoGastoId_Mes_Ano_Activos ON Presupuestos;
                        PRINT 'Índice IX_Presupuestos_TipoGastoId_Mes_Ano_Activos eliminado';
                    END
                ");

                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE UNIQUE INDEX IX_Presupuestos_TipoGastoId_Mes_Ano_Activos 
                    ON Presupuestos (TipoGastoId, Mes, Ano) 
                    WHERE Activo = 1;
                    PRINT 'Nuevo índice filtrado creado: IX_Presupuestos_TipoGastoId_Mes_Ano_Activos';
                ");

                return Json(new
                {
                    success = true,
                    mensaje = "ÍNDICE ARREGLADO DEFINITIVAMENTE",
                    detalles = "Eliminados índices anteriores y creado nuevo índice filtrado correcto",
                    instrucciones = "Ahora puedes crear -> eliminar -> recrear presupuestos sin problemas"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR AL ARREGLAR ÍNDICE: {ex.Message}");
                return Json(new
                {
                    success = false,
                    error = $"Error al arreglar índice: {ex.Message}",
                    solucionAlternativa = "Usa LimpiarTodosInactivos para eliminar registros problemáticos"
                });
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> VerTodosActivos()
        {
            try
            {
                var activos = await _context.Presupuestos
                    .Where(p => p.Activo)
                    .Include(p => p.TipoGasto)
                    .OrderBy(p => p.Ano)
                    .ThenBy(p => p.Mes)
                    .ThenBy(p => p.TipoGasto.Nombre)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"=== REGISTROS ACTIVOS ENCONTRADOS: {activos.Count} ===");
                foreach (var registro in activos)
                {
                    System.Diagnostics.Debug.WriteLine($"ID: {registro.Id}, TipoGasto: {registro.TipoGasto?.Nombre}, Mes: {registro.Mes}, Año: {registro.Ano}, Monto: {registro.MontoPresupuestado}");
                }

                return Json(new
                {
                    total = activos.Count,
                    presupuestos = activos.Select(p => new {
                        p.Id,
                        p.TipoGastoId,
                        TipoGasto = p.TipoGasto.Nombre,
                        p.Mes,
                        p.Ano,
                        p.MontoPresupuestado,
                        p.Activo
                    })
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR AL VER ACTIVOS: {ex.Message}");
                return Json(new { error = ex.Message });
            }
        }

        // ===== MÉTODOS PRINCIPALES =====
        public async Task<IActionResult> Index()
        {
            var presupuestos = _context.Presupuestos
                .Include(p => p.TipoGasto)
                .Where(p => p.Activo)
                .OrderByDescending(p => p.Ano)
                .ThenByDescending(p => p.Mes);
            return View(await presupuestos.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var presupuesto = await _context.Presupuestos
                .Include(p => p.TipoGasto)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (presupuesto == null) return NotFound();

            return View(presupuesto);
        }

        public IActionResult Create()
        {
            var tiposGasto = _context.TiposGasto.Where(t => t.Activo).ToList();
            ViewData["TipoGastoId"] = new SelectList(tiposGasto, "Id", "Nombre");
            ViewBag.Meses = GetMesesSelectList();
            return View(new Presupuesto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int TipoGastoId, int Mes, int Ano, decimal MontoPresupuestado)
        {
            System.Diagnostics.Debug.WriteLine($"=== CREATE POST - TipoGasto: {TipoGastoId}, Mes: {Mes}, Año: {Ano}, Monto: {MontoPresupuestado} ===");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool esValido = true;
                var errores = new List<string>();

                if (TipoGastoId <= 0)
                {
                    errores.Add("Debe seleccionar un tipo de gasto");
                    esValido = false;
                }

                if (Mes < 1 || Mes > 12)
                {
                    errores.Add("El mes debe estar entre 1 y 12");
                    esValido = false;
                }

                if (Ano < DateTime.Now.Year - 5 || Ano > DateTime.Now.Year + 5)
                {
                    errores.Add("El año debe estar en un rango válido");
                    esValido = false;
                }

                if (MontoPresupuestado <= 0)
                {
                    errores.Add("El monto debe ser mayor a cero");
                    esValido = false;
                }

                if (!esValido)
                {
                    await transaction.RollbackAsync();
                    foreach (var error in errores)
                    {
                        ModelState.AddModelError("", error);
                        System.Diagnostics.Debug.WriteLine($"Create - Error validación: {error}");
                    }
                }
                else
                {
                    var registrosInactivos = await _context.Presupuestos
                        .Where(p => !p.Activo
                                   && p.TipoGastoId == TipoGastoId
                                   && p.Mes == Mes
                                   && p.Ano == Ano)
                        .ToListAsync();

                    if (registrosInactivos.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Create - Limpiando {registrosInactivos.Count} registros inactivos previos");
                        _context.Presupuestos.RemoveRange(registrosInactivos);
                        await _context.SaveChangesAsync();
                    }

                    var existeActivo = await _context.Presupuestos
                        .AnyAsync(p => p.TipoGastoId == TipoGastoId
                                      && p.Mes == Mes
                                      && p.Ano == Ano
                                      && p.Activo);

                    if (existeActivo)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Ya existe un presupuesto activo para este tipo de gasto en el mes y año seleccionado");
                        System.Diagnostics.Debug.WriteLine("Create - ERROR: Ya existe presupuesto activo");
                    }
                    else
                    {
                        // FIX SQLite: Crear presupuesto con monto ejecutado inicial
                        var gastosExistentes = await _context.Gastos
                            .Where(g => g.TipoGastoId == TipoGastoId
                                       && g.FechaGasto.Month == Mes
                                       && g.FechaGasto.Year == Ano
                                       && g.Activo)
                            .ToListAsync();

                        var totalGastadoExistente = gastosExistentes.Sum(g => g.Monto);

                        var presupuesto = new Presupuesto
                        {
                            TipoGastoId = TipoGastoId,
                            Mes = Mes,
                            Ano = Ano,
                            MontoPresupuestado = MontoPresupuestado,
                            FechaCreacion = DateTime.Now,
                            Activo = true,
                            MontoEjecutado = totalGastadoExistente // Inicializar con gastos existentes
                        };

                        _context.Add(presupuesto);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        System.Diagnostics.Debug.WriteLine($"Create - Presupuesto creado con MontoEjecutado inicial: {totalGastadoExistente}");
                        TempData["SuccessMessage"] = "Presupuesto creado exitosamente";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                System.Diagnostics.Debug.WriteLine($"Create - ERROR: {ex.Message}");
                ModelState.AddModelError("", $"Error al crear presupuesto: {ex.Message}");
            }

            var tiposGasto = _context.TiposGasto.Where(t => t.Activo).ToList();
            ViewData["TipoGastoId"] = new SelectList(tiposGasto, "Id", "Nombre", TipoGastoId);
            ViewBag.Meses = GetMesesSelectList(Mes);

            return View(new Presupuesto
            {
                TipoGastoId = TipoGastoId,
                Mes = Mes,
                Ano = Ano,
                MontoPresupuestado = MontoPresupuestado
            });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var presupuesto = await _context.Presupuestos.FindAsync(id);
            if (presupuesto == null) return NotFound();

            ViewData["TipoGastoId"] = new SelectList(_context.TiposGasto.Where(t => t.Activo), "Id", "Nombre", presupuesto.TipoGastoId);
            ViewBag.Meses = GetMesesSelectList(presupuesto.Mes);
            return View(presupuesto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int TipoGastoId, int Mes, int Ano, decimal MontoPresupuestado, decimal? MontoEjecutado, DateTime FechaCreacion, bool Activo)
        {
            System.Diagnostics.Debug.WriteLine($"=== EDIT POST INICIANDO - ID: {id} ===");
            System.Diagnostics.Debug.WriteLine($"Valores recibidos: TipoGasto={TipoGastoId}, Mes={Mes}, Año={Ano}, MontoPresupuestado={MontoPresupuestado}");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var presupuesto = await _context.Presupuestos.FindAsync(id);
                if (presupuesto == null)
                {
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"ERROR: No se encontró presupuesto con ID {id}");
                    return NotFound();
                }

                System.Diagnostics.Debug.WriteLine($"Presupuesto actual: TipoGasto={presupuesto.TipoGastoId}, Mes={presupuesto.Mes}, Año={presupuesto.Ano}, Monto={presupuesto.MontoPresupuestado}");

                // Guardar valores anteriores para recalcular si es necesario
                var tipoGastoAnterior = presupuesto.TipoGastoId;
                var mesAnterior = presupuesto.Mes;
                var anoAnterior = presupuesto.Ano;

                bool esValido = true;
                var errores = new List<string>();

                // Validaciones
                if (TipoGastoId <= 0)
                {
                    errores.Add("Debe seleccionar un tipo de gasto");
                    esValido = false;
                }

                if (Mes < 1 || Mes > 12)
                {
                    errores.Add("El mes debe estar entre 1 y 12");
                    esValido = false;
                }

                if (MontoPresupuestado <= 0)
                {
                    errores.Add("El monto presupuestado debe ser mayor a cero");
                    esValido = false;
                }

                // Verificar que no exista otro presupuesto activo para el mismo periodo
                var existePresupuesto = await _context.Presupuestos
                    .AnyAsync(p => p.TipoGastoId == TipoGastoId
                                && p.Mes == Mes
                                && p.Ano == Ano
                                && p.Activo
                                && p.Id != id);

                if (existePresupuesto)
                {
                    errores.Add("Ya existe otro presupuesto activo para este tipo de gasto en el mes y año seleccionado");
                    esValido = false;
                }

                if (!esValido)
                {
                    await transaction.RollbackAsync();
                    foreach (var error in errores)
                    {
                        ModelState.AddModelError("", error);
                        System.Diagnostics.Debug.WriteLine($"Edit - Error de validación: {error}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Actualizando presupuesto: {tipoGastoAnterior},{mesAnterior},{anoAnterior} → {TipoGastoId},{Mes},{Ano}");

                    // Actualizar las propiedades del presupuesto
                    presupuesto.TipoGastoId = TipoGastoId;
                    presupuesto.Mes = Mes;
                    presupuesto.Ano = Ano;
                    presupuesto.MontoPresupuestado = MontoPresupuestado;
                    presupuesto.FechaCreacion = FechaCreacion;
                    presupuesto.Activo = true;

                    // FIX SQLite: Calcular el nuevo MontoEjecutado basado en gastos reales del NUEVO periodo
                    var gastosNuevoPeriodo = await _context.Gastos
                        .Where(g => g.TipoGastoId == TipoGastoId
                                   && g.FechaGasto.Month == Mes
                                   && g.FechaGasto.Year == Ano
                                   && g.Activo)
                        .ToListAsync();

                    var totalGastadoNuevoPeriodo = gastosNuevoPeriodo.Sum(g => g.Monto);
                    presupuesto.MontoEjecutado = totalGastadoNuevoPeriodo;

                    System.Diagnostics.Debug.WriteLine($"MontoEjecutado calculado para nuevo periodo: {totalGastadoNuevoPeriodo}");

                    // Actualizar el presupuesto
                    _context.Update(presupuesto);
                    await _context.SaveChangesAsync();

                    // Si cambió el periodo, actualizar el presupuesto del periodo anterior (por si quedaron gastos allí)
                    if (tipoGastoAnterior != TipoGastoId || mesAnterior != Mes || anoAnterior != Ano)
                    {
                        System.Diagnostics.Debug.WriteLine("El periodo cambió, recalculando presupuesto del periodo anterior...");

                        var presupuestoAnterior = await _context.Presupuestos
                            .FirstOrDefaultAsync(p => p.TipoGastoId == tipoGastoAnterior
                                               && p.Mes == mesAnterior
                                               && p.Ano == anoAnterior
                                               && p.Activo
                                               && p.Id != id);

                        if (presupuestoAnterior != null)
                        {
                            // FIX SQLite: Recalcular periodo anterior
                            var gastosPeriodoAnterior = await _context.Gastos
                                .Where(g => g.TipoGastoId == tipoGastoAnterior
                                           && g.FechaGasto.Month == mesAnterior
                                           && g.FechaGasto.Year == anoAnterior
                                           && g.Activo)
                                .ToListAsync();

                            var totalGastadoPeriodoAnterior = gastosPeriodoAnterior.Sum(g => g.Monto);

                            presupuestoAnterior.MontoEjecutado = totalGastadoPeriodoAnterior;
                            _context.Update(presupuestoAnterior);
                            await _context.SaveChangesAsync();

                            System.Diagnostics.Debug.WriteLine($"Presupuesto anterior actualizado - MontoEjecutado: {totalGastadoPeriodoAnterior}");
                        }
                    }

                    await transaction.CommitAsync();
                    System.Diagnostics.Debug.WriteLine("Edit - Presupuesto actualizado exitosamente");
                    TempData["SuccessMessage"] = "Presupuesto actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                System.Diagnostics.Debug.WriteLine($"Edit - ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Edit - StackTrace: {ex.StackTrace}");
                ModelState.AddModelError("", $"Error al actualizar el presupuesto: {ex.Message}");
            }

            // Si hay errores, recargar la vista
            ViewData["TipoGastoId"] = new SelectList(_context.TiposGasto.Where(t => t.Activo), "Id", "Nombre", TipoGastoId);
            ViewBag.Meses = GetMesesSelectList(Mes);

            var presupuestoError = new Presupuesto
            {
                Id = id,
                TipoGastoId = TipoGastoId,
                Mes = Mes,
                Ano = Ano,
                MontoPresupuestado = MontoPresupuestado,
                MontoEjecutado = MontoEjecutado ?? 0,
                FechaCreacion = FechaCreacion,
                Activo = true
            };
            return View(presupuestoError);
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
            System.Diagnostics.Debug.WriteLine($"=== DELETE POST - ID: {id} ===");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var presupuesto = await _context.Presupuestos.FindAsync(id);
                if (presupuesto != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Delete - Eliminando presupuesto: TipoGasto={presupuesto.TipoGastoId}, Mes={presupuesto.Mes}, Año={presupuesto.Ano}");

                    _context.Presupuestos.Remove(presupuesto);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    System.Diagnostics.Debug.WriteLine("Delete - Eliminación exitosa");
                    TempData["SuccessMessage"] = "Presupuesto eliminado exitosamente";
                }
                else
                {
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"Delete - ERROR: No se encontró presupuesto con ID {id}");
                    TempData["ErrorMessage"] = "No se encontró el presupuesto a eliminar";
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                System.Diagnostics.Debug.WriteLine($"Delete - ERROR: {ex.Message}");
                TempData["ErrorMessage"] = $"Error al eliminar: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Reporte(int? ano, int? mes)
        {
            var query = _context.Presupuestos
                .Include(p => p.TipoGasto)
                .Where(p => p.Activo);

            if (ano.HasValue)
                query = query.Where(p => p.Ano == ano.Value);
            if (mes.HasValue)
                query = query.Where(p => p.Mes == mes.Value);

            var presupuestos = await query.OrderBy(p => p.TipoGasto.Nombre).ToListAsync();

            ViewBag.AnoSeleccionado = ano ?? DateTime.Now.Year;
            ViewBag.MesSeleccionado = mes;
            ViewBag.Meses = GetMesesSelectList(mes);

            return View(presupuestos);
        }

        #region Métodos Privados

        private bool PresupuestoExists(int id)
        {
            return _context.Presupuestos.Any(e => e.Id == id);
        }

        private SelectList GetMesesSelectList(int? selectedValue = null)
        {
            return new SelectList(new[]
            {
                new { Value = 1, Text = "Enero" },
                new { Value = 2, Text = "Febrero" },
                new { Value = 3, Text = "Marzo" },
                new { Value = 4, Text = "Abril" },
                new { Value = 5, Text = "Mayo" },
                new { Value = 6, Text = "Junio" },
                new { Value = 7, Text = "Julio" },
                new { Value = 8, Text = "Agosto" },
                new { Value = 9, Text = "Septiembre" },
                new { Value = 10, Text = "Octubre" },
                new { Value = 11, Text = "Noviembre" },
                new { Value = 12, Text = "Diciembre" }
            }, "Value", "Text", selectedValue);
        }

        #endregion
    }
}

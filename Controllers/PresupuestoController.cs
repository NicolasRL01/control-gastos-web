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

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Presupuesto modelo)
        {
            return RedirectToAction("Index");
        }

        public IActionResult Reporte()
        {
            return View();
        }
    }
}

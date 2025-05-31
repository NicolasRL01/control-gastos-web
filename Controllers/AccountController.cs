using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace ControlGastosWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                // Intentar login con username
                var result = await _signInManager.PasswordSignInAsync(username, password, false, false);

                if (result.Succeeded)
                {
                    // Login exitoso - redirigir a Home
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.Error = "Usuario o contraseña incorrectos.";
                }
            }
            else
            {
                ViewBag.Error = "Por favor ingrese usuario y contraseña.";
            }

            return View();
        }

        // POST: Account/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
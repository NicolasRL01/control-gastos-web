using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ControlGastosWeb.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // AGREGAR ESTA LÍNEA

// Configurar Entity Framework - CAMBIADO A SQLITE
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurar Identity - PERMITIR LOGIN CON USERNAME
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Configuraciones de contraseña (menos restrictivas)
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 5;
    options.Password.RequiredUniqueChars = 1;

    // PERMITIR LOGIN CON USERNAME (NO EMAIL)
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// IMPORTANTE: Orden correcto de Authentication y Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Agregar rutas de Identity (Login, Register, etc.)
app.MapRazorPages();

// Asegurar que la base de datos esté creada y crear usuario admin
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    try
    {
        // MÉTODO PARA SQLITE - Crear base de datos si no existe
        context.Database.EnsureCreated();
        Console.WriteLine("Base de datos SQLite creada/verificada exitosamente.");

        // Crear usuario admin si no existe
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = "admin",
                Email = "admin@localhost.com",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "admin");
            if (result.Succeeded)
            {
                Console.WriteLine("Usuario admin creado exitosamente.");
                Console.WriteLine("Usuario: admin");
                Console.WriteLine("Contraseña: admin");
            }
            else
            {
                Console.WriteLine("Error creando usuario admin:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"- {error.Description}");
                }
            }
        }
        else
        {
            Console.WriteLine("Usuario admin ya existe.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error en configuración inicial: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

app.Run();
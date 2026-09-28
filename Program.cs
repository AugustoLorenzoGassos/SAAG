using Microsoft.EntityFrameworkCore;
using Seguimiento.Models;
using System.Diagnostics;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//Add service for session management
builder.Services.AddDistributedMemoryCache(); // Requerido para almacenar la sesión en memoria
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de expiración de la sesión
    options.Cookie.HttpOnly = true;                 // Previene acceso desde JS del cliente por seguridad
    options.Cookie.IsEssential = true;             // Necesario si se usan políticas de consentimiento de cookies
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<SedarpaContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddHttpContextAccessor(); // Agregar el servicio IHttpContextAccessor

builder.Services.AddHttpClient("PythonApi", client =>
{
    client.BaseAddress = new Uri("http://127.0.0.1:8000/");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Seguimiento}/{action=Inicio}/{id?}/{id1?}/{id2?}")
    .WithStaticAssets();

app.Run();

// Método para verificar si el servidor de Python está activo
static bool IsPortInUse(int port)
{
    try
    {
        using var client = new TcpClient("127.0.0.1", port);
        return true;
    }
    catch
    {
        return false;
    }
}

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PedidosPendientes.Web.Models;

namespace PedidosPendientes.Web.Controllers;

public class HomeController : Controller
{
    // Punto de entrada: redirige al listado de pedidos pendientes.
    public IActionResult Index() => RedirectToAction("Index", "Orders");

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

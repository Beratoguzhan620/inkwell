using BlogApp.Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogApp.Admin.Controllers;

public class HomeController(IAdminService adminService) : AdminControllerBase
{
    public async Task<IActionResult> Index() => View(await adminService.GetDashboardAsync());

    [AllowAnonymous]
    [Route("/not-found")]
    public IActionResult PageNotFound()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("Status", ("Sayfa bulunamadı", "Aradığınız kayıt silinmiş ya da adres hatalı olabilir.", "bi-signpost-split"));
    }

    [AllowAnonymous]
    [Route("/error")]
    public IActionResult Error() =>
        View("Status", ("Bir hata oluştu", "İsteğiniz işlenirken beklenmeyen bir hata oluştu. Biraz sonra tekrar deneyin.", "bi-exclamation-triangle"));
}

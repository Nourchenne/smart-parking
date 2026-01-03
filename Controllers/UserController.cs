using System.Threading.Tasks;
using auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace auth.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserParkingService _userParkingService;

        public UserController(SignInManager<IdentityUser> signInManager, UserParkingService userParkingService)
        {
            _signInManager = signInManager;
            _userParkingService = userParkingService;
        }

        // GET: /User/Dashboard?q=
        [HttpGet]
        public async Task<IActionResult> Dashboard(string? q)
        {
            ViewBag.Q = q;
            var parkings = await _userParkingService.SearchParkingsAsync(q);
            return View(parkings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}

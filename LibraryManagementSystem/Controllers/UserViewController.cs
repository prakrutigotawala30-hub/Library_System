using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "User")]
    public class UserViewController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
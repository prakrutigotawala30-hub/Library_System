using Library_Management_System.ViewModels;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileController(IWebHostEnvironment env,UserManager<ApplicationUser> userManager,SignInManager<ApplicationUser> signInManager)
        {
            _env = env;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // PROFILE PAGE
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new ProfileViewModel
            {
                Name = user.FullName,
                Phone = user.PhoneNumber,
                NotificationPrefs = user.NotificationPrefs,
                ProfileImagePath = user.ProfileImagePath
            };

            return View(model);
        }

        // EDIT PROFILE GET

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new ProfileViewModel
            {
                Name = user.FullName,
                Phone = user.PhoneNumber,
                NotificationPrefs = user.NotificationPrefs,
                ProfileImagePath = user.ProfileImagePath
            };

            return View(model);
        }

        // EDIT PROFILE POST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            user.FullName = model.Name;
            user.PhoneNumber = model.Phone;
            user.NotificationPrefs = model.NotificationPrefs;

            await _userManager.UpdateAsync(user);

            TempData["success"] =
                "Profile updated successfully";

            return RedirectToAction(nameof(Index));
        }

        // AVATAR PAGE GET

        [HttpGet]
        public async Task<IActionResult> Avatar()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new ProfileViewModel
            {
                ProfileImagePath = user.ProfileImagePath
            };

            return View(model);
        }

        // AVATAR UPLOAD POST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Avatar(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            if (model.AvatarFile != null)
            {
                var folder = Path.Combine(
                    _env.WebRootPath,
                    "images");

                Directory.CreateDirectory(folder);

                var fileName =
                    Guid.NewGuid().ToString() +
                    Path.GetExtension(model.AvatarFile.FileName);

                var path = Path.Combine(folder, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(stream);
                }

                user.ProfileImagePath =
                    "/images/" + fileName;

                await _userManager.UpdateAsync(user);

                TempData["success"] =
                    "Profile photo updated successfully";
            }

            return RedirectToAction(nameof(Edit));
        }

        // CHANGE PASSWORD GET

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }
        // CHANGE PASSWORD POST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var result =
                await _userManager.ChangePasswordAsync(
                    user,
                    model.CurrentPassword,
                    model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View(model);
            }

            await _signInManager.SignOutAsync();

            TempData["success"] =
                "Password updated successfully";

            return RedirectToAction(
                "Login",
                "Account");
        }
    }
}
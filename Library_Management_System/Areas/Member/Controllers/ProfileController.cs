using Library_Management_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public ProfileController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // Edit GET

        public IActionResult Edit()
        {
            var model = new ProfileViewModel
            {
                Name = "Prakruti",
                Phone = "9876543210",
                NotificationPrefs = true
            };

            return View(model);
        }

        // Edit POST

        [HttpPost]
        public IActionResult Edit(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            TempData["success"] = "Profile Updated";

            return RedirectToAction(nameof(Edit));
        }


        // Change Password GET

        public IActionResult ChangePassword()
        {
            return View();
        }

        // Change Password POST

        [HttpPost]
        public IActionResult ChangePassword(
            string CurrentPassword,
            string NewPassword,
            string ConfirmPassword)
        {
            if (NewPassword != ConfirmPassword)
            {
                ModelState.AddModelError(
                    "",
                    "Passwords do not match");

                return View();
            }

            TempData["success"] =
                "Password Changed Successfully";

            return RedirectToAction(nameof(Edit));
        }


        // Avatar GET

        public IActionResult Avatar()
        {
            return View();
        }


        // Upload Avatar

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(
            ProfileViewModel model)
        {
            if (model.AvatarFile != null)
            {
                var folder = Path.Combine(
                    _env.WebRootPath,
                    "uploads");

                Directory.CreateDirectory(folder);

                var fileName =
                    Guid.NewGuid() +
                    Path.GetExtension(
                    model.AvatarFile.FileName);

                var path =
                    Path.Combine(folder, fileName);

                using var stream =
                    new FileStream(
                        path,
                        FileMode.Create);

                await model.AvatarFile
                    .CopyToAsync(stream);

                TempData["img"] =
                    "/uploads/" + fileName;
            }

            return RedirectToAction(nameof(Avatar));
        }
    }
}
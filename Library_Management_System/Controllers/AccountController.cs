using Library_Management_System.Models;
using Library_Management_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly SignInManager<ApplicationUser> _signInManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;

            _signInManager = signInManager;

            _roleManager = roleManager;
        }

        // REGISTER

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // CREATE MEMBER ROLE
                if (!await _roleManager.RoleExistsAsync("Member"))
                {
                    await _roleManager.CreateAsync(
                        new IdentityRole("Member"));
                }

                // CHECK EMAIL ALREADY EXISTS
                var existingUser =
                    await _userManager.FindByEmailAsync(model.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError(
                        "Email",
                        "Email already registered.");

                    return View(model);
                }

                var user = new ApplicationUser
                {
                    FullName = model.FullName,
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address
                };

                var result =
                    await _userManager.CreateAsync(
                        user,
                        model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(
                        user,
                        "Member");

                    TempData["success"] =
                        "Registration successful. Please login.";

                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }
            }

            return View(model);
        }

        // LOGIN

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // CHECK USER EXISTS
                var user =
                    await _userManager.FindByEmailAsync(
                        model.Email);

                if (user == null)
                {
                    ModelState.AddModelError(
                        "Email",
                        "Account not found. Please register first.");

                    return View(model);
                }

                var result =
                    await _signInManager.PasswordSignInAsync(
                        model.Email,
                        model.Password,
                        model.RememberMe,
                        false);

                if (result.Succeeded)
                {
                    TempData["success"] =
                        "Login successful.";

                    // REDIRECT HOME
                    return RedirectToAction(
                        "Index",
                        "Home");
                }

                // WRONG PASSWORD
                ModelState.AddModelError(
                    "Password",
                    "Incorrect password.");
            }

            return View(model);
        }

        // LOGOUT

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            TempData["success"] =
                "Logged out successfully.";

            return RedirectToAction("Login");
        }

        // FORGOT PASSWORD

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user =
                    await _userManager.FindByEmailAsync(
                        model.Email);

                if (user == null)
                {
                    TempData["error"] =
                        "User not found.";

                    return View(model);
                }

                var token =
                    await _userManager
                        .GeneratePasswordResetTokenAsync(user);

                var resetLink =
                    Url.Action(
                        "ResetPassword",
                        "Account",
                        new
                        {
                            token,
                            email = user.Email
                        },
                        Request.Scheme);

                TempData["success"] =
                    "Reset link generated.";

                // TEMP DISPLAY
                ViewBag.ResetLink = resetLink;

                return View();
            }

            return View(model);
        }

        // RESET PASSWORD

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(
            string token,
            string email)
        {
            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user =
                    await _userManager.FindByEmailAsync(
                        model.Email);

                if (user == null)
                {
                    TempData["error"] =
                        "User not found.";

                    return RedirectToAction("Login");
                }

                var result =
                    await _userManager.ResetPasswordAsync(
                        user,
                        model.Token,
                        model.Password);

                if (result.Succeeded)
                {
                    TempData["success"] =
                        "Password reset successful.";

                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }
            }

            return View(model);
        }
    }
}
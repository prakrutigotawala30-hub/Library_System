using LibraryManagementSystem.Models;
using Library_Management_System.ViewModels;
using Library_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LibraryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        // ================= REGISTER =================

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
                if (!await _roleManager.RoleExistsAsync("Member"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Member"));
                }

                var existingUser = await _userManager.FindByEmailAsync(model.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Email already registered.");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    FullName = model.FullName,
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    EmailConfirmed = false
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Member");

                    // Generate confirmation token + link, then email it. We do NOT
                    // sign the user in here — they must click the link first.
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                    var confirmLink = Url.Action(
                        "ConfirmEmail",
                        "Account",
                        new
                        {
                            userId = user.Id,
                            token = WebUtility.UrlEncode(token)
                        },
                        Request.Scheme);

                    string subject = "Confirm your Library System account";
                    string body = $@"
                        <h3>Welcome, {user.FullName}!</h3>
                        <p>Please confirm your email to activate your account:</p>
                        <p><a href='{confirmLink}'>Confirm Email</a></p>
                        <p>If the button doesn't work, copy this link into your browser:</p>
                        <p>{confirmLink}</p>
                    ";

                    try
                    {
                        await _emailService.SendEmailAsync(user.Email, subject, body);
                        TempData["success"] =
                            "Account created. Please check your email and click the confirmation link to activate it.";
                    }
                    catch
                    {
                        // SMTP failed — account exists but no email sent.
                        // Give the user actionable feedback rather than a silent error.
                        TempData["error"] =
                            "Account created but confirmation email could not be sent. " +
                            "Please contact the administrator.";
                    }

                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }

        // ================= EMAIL CONFIRM =================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                TempData["error"] = "Invalid confirmation link.";
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["error"] = "Account not found.";
                return RedirectToAction("Login");
            }

            // Token came through the URL with WebUtility.UrlEncode applied —
            // ASP.NET model binding already URL-decodes once when reading from
            // the query string, so the token here is the original. No extra decode.
            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                TempData["success"] = "Email confirmed. You can now log in.";
                return View();
            }

            TempData["error"] = "Email confirmation failed. The link may have expired.";
            return RedirectToAction("Login");
        }

        // ================= LOGIN =================

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
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("", "Account not found.");
                    return View(model);
                }

                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError(
                        "",
                        "Email not confirmed yet. Please check your inbox for the confirmation link.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName,
                    model.Password,
                    model.RememberMe,
                    false);

                if (result.Succeeded)
                {
                    TempData["success"] = "Login successful.";
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Invalid login attempt.");
            }

            return View(model);
        }

        // ================= LOGOUT =================

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["success"] = "Logged out successfully.";
            return RedirectToAction("Login");
        }

        // ================= FORGOT PASSWORD =================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    TempData["error"] = "User not found.";
                    return View(model);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var resetLink = Url.Action(
                    "ResetPassword",
                    "Account",
                    new
                    {
                        token = WebUtility.UrlEncode(token),
                        email = user.Email
                    },
                    Request.Scheme
                );

                string subject = "Password Reset Link";
                string body = $@"
                    <h3>Password Reset Request</h3>
                    <p>Click below to reset your password:</p>
                    <a href='{resetLink}'>Reset Password</a>
                ";

                try
                {
                    await _emailService.SendEmailAsync(user.Email, subject, body);
                    TempData["success"] = "Reset link sent to email.";
                }
                catch
                {
                    TempData["error"] = "Could not send reset email. Try again later.";
                }

                return View();
            }

            return View(model);
        }

        // ================= RESET PASSWORD =================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            return View(new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    TempData["error"] = "User not found.";
                    return RedirectToAction("Login");
                }

                model.Token = WebUtility.UrlDecode(model.Token);

                var result = await _userManager.ResetPasswordAsync(
                    user,
                    model.Token,
                    model.Password);

                if (result.Succeeded)
                {
                    TempData["success"] = "Password reset successful.";
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }
    }
}
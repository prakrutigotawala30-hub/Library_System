using System.IO;
using Library_Management_System.ViewModels;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Controllers
{
    [Authorize(Roles = "User,Member")]
    public class MembershipController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly RoleManager<IdentityRole> _roleManager;

        public MembershipController(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IConfiguration configuration,
    RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var member = await _context.Members
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == user.Id);

            if (member != null)
            {
                // APPROVED MEMBERSHIP
                var activeMembership = await _context.Memberships
                    .AnyAsync(x =>
                        x.MemberId == member.Id &&
                        x.IsActive &&
                        x.EndDate >= DateTime.Now);

                if (activeMembership)
                {
                    return RedirectToAction(
                        "Index",
                        "Dashboard",
                        new { area = "Member" });
                }

                // PENDING APPROVAL
                var pendingMembership = await _context.Memberships
                    .AnyAsync(x =>
                        x.MemberId == member.Id &&
                        !x.IsActive);

                if (pendingMembership)
                {
                    return RedirectToAction(
                        nameof(PendingApproval));
                }
            }

            // SHOW MEMBERSHIP PLANS
            return View();
        }
        // BUY MEMBERSHIP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(
    string membershipType,
    int durationMonths)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var member = await _context.Members
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == user.Id);

            if (member != null)
            {
                // Active Membership
                var activeMembership = await _context.Memberships
                    .AnyAsync(x =>
                        x.MemberId == member.Id &&
                        x.IsActive &&
                        x.EndDate >= DateTime.Now);

                if (activeMembership)
                {
                    TempData["Error"] =
                        "You already have an active membership.";

                    return RedirectToAction(nameof(Index));
                }

                // Pending Membership
                var pendingMembership = await _context.Memberships
                    .AnyAsync(x =>
                        x.MemberId == member.Id &&
                        !x.IsActive);

                if (pendingMembership)
                {
                    TempData["Error"] =
                        "Your membership request is already awaiting admin approval.";

                    return RedirectToAction(
                        nameof(PendingApproval));
                }
            }

            decimal fee = 0;

            if (membershipType == "Student")
                fee = durationMonths == 1 ? 99 : 1000;

            else if (membershipType == "Regular")
                fee = durationMonths == 1 ? 149 : 1500;

            else if (membershipType == "Premium")
                fee = durationMonths == 1 ? 299 : 3000;

            TempData["MembershipType"] = membershipType;
            TempData["DurationMonths"] = durationMonths;
            TempData["Fee"] = fee.ToString();

            return RedirectToAction(nameof(Checkout));
        }

        // CHECKOUT
        [HttpGet]
        public IActionResult Checkout()
        {
            if (TempData["MembershipType"] == null ||
                TempData["DurationMonths"] == null ||
                TempData["Fee"] == null)
            {
                return RedirectToAction(nameof(Index));
            }

            string membershipType =
                TempData["MembershipType"]?.ToString();

            int durationMonths =
                Convert.ToInt32(
                    TempData["DurationMonths"]);

            decimal fee =
                Convert.ToDecimal(
                    TempData["Fee"]?.ToString());

            TempData.Keep();

            var razorpayKey =
                _configuration["Razorpay:Key"];

            var razorpaySecret =
                _configuration["Razorpay:Secret"];

            var client =
                new Razorpay.Api.RazorpayClient(
                    razorpayKey,
                    razorpaySecret);

            Dictionary<string, object> options =
                new();

            options.Add(
                "amount",
                Convert.ToInt32(fee * 100));

            options.Add(
                "currency",
                "INR");

            options.Add(
                "receipt",
                Guid.NewGuid().ToString());

            var order =
                client.Order.Create(options);

            var model =
                new MembershipRazorPayViewModel
                {
                    MembershipType = membershipType,
                    DurationMonths = durationMonths,
                    Amount = fee,
                    RazorpayKey = razorpayKey,
                    RazorpayOrderId = order["id"].ToString()
                };

            return View(model);
        }

        // PAYMENT SUCCESS (USER SUBMISSION)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaymentSuccess(
    string razorpayPaymentId,
    string razorpayOrderId,
    string razorpaySignature)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            string membershipType =
                TempData["MembershipType"]?.ToString();

            int durationMonths =
                Convert.ToInt32(
                    TempData["DurationMonths"]);

            decimal fee =
                Convert.ToDecimal(
                    TempData["Fee"].ToString());

            var member =
                await _context.Members
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == user.Id);

            if (member == null)
            {
                member = new Member
                {
                    ApplicationUserId = user.Id,
                    Name = user.FullName,
                    Email = user.Email,
                    Phone = user.PhoneNumber
                };

                _context.Members.Add(member);

                await _context.SaveChangesAsync();
            }

            var membership =
                new Membership
                {
                    MemberId = member.Id,
                    MembershipType = membershipType,
                    DurationMonths = durationMonths,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(durationMonths),
                    Fee = fee,

                    // WAITING FOR ADMIN
                    IsActive = false
                };

            _context.Memberships.Add(membership);

            await _context.SaveChangesAsync();

            var payment =
                new MembershipPayment
                {
                    MembershipId = membership.Id,
                    Amount = fee,
                    PaymentMethod = "Razorpay",

                    // WAITING FOR APPROVAL
                    PaymentStatus = "Pending",

                    TransactionId = razorpayPaymentId,
                    PaymentDate = DateTime.Now
                };

            _context.MembershipPayments.Add(payment);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Payment successful. Your membership request has been submitted and is awaiting admin approval.";

            return RedirectToAction(
                nameof(PendingApproval));
        }

        // SUCCESS PAGE
        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }

        [HttpGet]
        public IActionResult PendingApproval()
        {
            return View();
        }

    }
}

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
    var user =
        await _userManager.GetUserAsync(User);

    if (user == null)
    {
        return RedirectToAction(
            "Login",
            "Account");
    }

    // CHECK ACTIVE MEMBERSHIP FROM DATABASE
    var member =
        await _context.Members
        .FirstOrDefaultAsync(x =>
            x.ApplicationUserId == user.Id);

    if (member != null)
    {
        var activeMembership =
            await _context.Memberships
            .AnyAsync(x =>
                x.MemberId == member.Id &&
                x.IsActive &&
                x.EndDate >= DateTime.Now);

        // ONLY REDIRECT IF MEMBERSHIP APPROVED + ACTIVE
        if (activeMembership)
        {
            return RedirectToAction(
                "Index",
                "Dashboard",
                new { area = "Member" });
        }
    }

    return View();
}
        // BUY MEMBERSHIP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Buy(string membershipType, int durationMonths)
        {
            decimal fee = 0;

            if (membershipType == "Student")
                fee = durationMonths == 1 ? 99 : 1000;

            else if (membershipType == "Regular")
                fee = durationMonths == 1 ? 149 : 1500;

            else if (membershipType == "Premium")
                fee = durationMonths == 1 ? 299 : 3000;

            TempData["MembershipType"] = membershipType;
            TempData["DurationMonths"] = durationMonths.ToString();
            TempData["Fee"] = fee.ToString();

            return RedirectToAction("Checkout");
        }

        // CHECKOUT
        [HttpGet]
        public IActionResult Checkout()
        {
            string membershipType =
                TempData["MembershipType"]?.ToString();

            int durationMonths =
                Convert.ToInt32(
                    TempData["DurationMonths"]);

            decimal fee =
                Convert.ToDecimal(
                    TempData["Fee"]);

            TempData.Keep();

            var razorpayKey =
                _configuration["Razorpay:Key"];

            var razorpaySecret =
                _configuration["Razorpay:Secret"];

            Razorpay.Api.RazorpayClient client =
                new Razorpay.Api.RazorpayClient(
                    razorpayKey,
                    razorpaySecret);

            Dictionary<string, object> options =
                new Dictionary<string, object>();

            options.Add(
                "amount",
                Convert.ToInt32(fee * 100));

            options.Add(
                "currency",
                "INR");

            options.Add(
                "receipt",
                Guid.NewGuid().ToString());

            Razorpay.Api.Order order =
                client.Order.Create(options);

            var model =
                new MembershipRazorPayViewModel
                {
                    MembershipType = membershipType,
                    DurationMonths = durationMonths,
                    Amount = fee,

                    RazorpayKey = razorpayKey,

                    RazorpayOrderId =
                        order["id"].ToString()
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
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            // ================= TEMP DATA =================
            string membershipType =
                TempData["MembershipType"]?.ToString();

            int durationMonths =
                Convert.ToInt32(TempData["DurationMonths"]);

            decimal fee =
                Convert.ToDecimal(TempData["Fee"]);

            TempData.Keep();

            // ================= MEMBER =================
            var member = await _context.Members
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

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

            // ================= MEMBERSHIP =================
            var membership = new Membership
            {
                MemberId = member.Id,
                MembershipType = membershipType,
                DurationMonths = durationMonths,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(durationMonths),
                Fee = fee,
                IsActive = false
            };

            _context.Memberships.Add(membership);
            await _context.SaveChangesAsync();

            // ================= PAYMENT =================
            var payment = new MembershipPayment
            {
                MembershipId = membership.Id,
                Amount = fee,
                PaymentMethod = "Razorpay",
                PaymentStatus = "Pending",
                TransactionId = razorpayPaymentId,
                PaymentDate = DateTime.Now
            };

            _context.MembershipPayments.Add(payment);
            await _context.SaveChangesAsync();

            // ================= ROLE UPDATE =================
            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Contains("Member"))
            {
                await _userManager.AddToRoleAsync(user, "Member");
            }

            // refresh login (IMPORTANT)
            await _signInManager.RefreshSignInAsync(user);

            // ================= SUCCESS MESSAGE =================
            TempData["Success"] = "Payment successful. Membership activated successfully.";

            // ================= REDIRECT =================
            return RedirectToAction(
                "Index",
                "Dashboard",
                new { area = "Member" });
        }

        // SUCCESS PAGE
        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }

        
    }
}

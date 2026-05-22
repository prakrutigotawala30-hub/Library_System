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

        public MembershipController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================= MEMBERSHIP PAGE =================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user =
                await _userManager.GetUserAsync(User);

            // already member
            if (await _userManager.IsInRoleAsync(user, "Member"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Member" });
            }

            return View();
        }

        // ================= BUY MEMBERSHIP =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(
            string membershipType,
            int durationMonths)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            // FIND MEMBER

            var member = await _context.Members
                .FirstOrDefaultAsync(x =>
                    x.Email == user.Email);

            // CREATE MEMBER IF NOT EXISTS

            if (member == null)
            {
                member = new Member
                {
                    Name = user.FullName,
                    Email = user.Email,
                    Phone = user.PhoneNumber
                };

                _context.Members.Add(member);

                await _context.SaveChangesAsync();
            }

            // CHECK ACTIVE MEMBERSHIP

            var alreadyActive =
                await _context.Memberships.AnyAsync(x =>
                    x.MemberId == member.Id &&
                    x.IsActive &&
                    x.EndDate > DateTime.Now);

            if (alreadyActive)
            {
                TempData["Error"] =
                    "You already have an active membership.";

                return RedirectToAction(nameof(Index));
            }

            decimal fee = 0;

            // ================= FEES =================

            if (membershipType == "Student")
            {
                if (durationMonths == 3)
                    fee = 400;

                else if (durationMonths == 6)
                    fee = 600;

                else if (durationMonths == 12)
                    fee = 1000;
            }
            else if (membershipType == "Regular")
            {
                if (durationMonths == 3)
                    fee = 600;

                else if (durationMonths == 6)
                    fee = 1000;

                else if (durationMonths == 12)
                    fee = 1500;
            }
            else if (membershipType == "Premium")
            {
                if (durationMonths == 3)
                    fee = 1000;

                else if (durationMonths == 6)
                    fee = 1800;

                else if (durationMonths == 12)
                    fee = 3000;
            }

            // CREATE MEMBERSHIP

            var membership = new Membership
            {
                MemberId = member.Id,
                MembershipType = membershipType,
                DurationMonths = durationMonths,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(durationMonths),
                Fee = fee,
                IsActive = true
            };

            _context.Memberships.Add(membership);

            await _context.SaveChangesAsync();

            // ================= ROLE UPDATE =================

            if (await _userManager.IsInRoleAsync(user, "User"))
            {
                await _userManager.RemoveFromRoleAsync(user, "User");
            }

            if (!await _userManager.IsInRoleAsync(user, "Member"))
            {
                await _userManager.AddToRoleAsync(user, "Member");
            }

            TempData["Success"] =
                "Membership activated successfully.";

            return RedirectToAction(
                "Index",
                "Dashboard",
                new { area = "Member" });
        }
    }
}

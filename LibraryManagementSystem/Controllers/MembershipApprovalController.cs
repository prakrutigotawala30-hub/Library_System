using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MembershipApprovalController : Controller
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser>
            _userManager;

        public MembershipApprovalController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;

            _userManager = userManager;
        }

        // MEMBERSHIP PAYMENTS LIST

        public async Task<IActionResult> Index()
        {
            var payments =
                await _context.MembershipPayments

                .Include(x => x.Membership)

                .ThenInclude(x => x.Member)

                .OrderByDescending(x => x.PaymentDate)

                .ToListAsync();

            return View(payments);
        }

        // APPROVE MEMBERSHIP

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var payment = await _context.MembershipPayments
                .Include(x => x.Membership)
                .ThenInclude(x => x.Member)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (payment == null)
            {
                TempData["Error"] = "Membership payment not found.";
                return RedirectToAction(nameof(Index));
            }

            if (payment.PaymentStatus == "Approved")
            {
                TempData["Error"] = "Membership is already approved.";
                return RedirectToAction(nameof(Index));
            }

            // Update status
            // payment.PaymentStatus = "Approved";
            // payment.Membership.MembershipStatus = "Active";

            payment.PaymentStatus = "Approved";

            payment.Membership.IsActive = true;
            payment.Membership.MembershipStatus = "Active";
            var member = payment.Membership.Member;

            var user = await _userManager.FindByIdAsync(
                member.ApplicationUserId);

            if (user != null)
            {
                // Remove User role
                if (await _userManager.IsInRoleAsync(user, "User"))
                {
                    await _userManager.RemoveFromRoleAsync(
                        user,
                        "User");
                }

                // Add Member role
                if (!await _userManager.IsInRoleAsync(user, "Member"))
                {
                    await _userManager.AddToRoleAsync(
                        user,
                        "Member");
                }

                // Create Notification
                _context.Notifications.Add(new Notification
                {
                    MemberId = user.Id,
                    Message = $"Congratulations! Your {payment.Membership.MembershipType} membership has been approved. You can now access all member features.",
                    Link = "/Area/Member/Dashboard/Index",
                    IsRead = false,
                    CreatedOn = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Membership approved for {member.Name}.";

            return RedirectToAction(nameof(Index));
        }

        // REJECT MEMBERSHIP

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var payment = await _context.MembershipPayments
                .Include(x => x.Membership)
                .ThenInclude(x => x.Member)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (payment == null)
            {
                TempData["Error"] = "Membership payment not found.";
                return RedirectToAction(nameof(Index));
            }

            if (payment.PaymentStatus == "Rejected")
            {
                TempData["Error"] = "Membership is already rejected.";
                return RedirectToAction(nameof(Index));
            }

            // Update status
            payment.PaymentStatus = "Rejected";

            payment.Membership.IsActive = false;
            payment.Membership.MembershipStatus = "Expired";

            var member = payment.Membership.Member;

            var user = await _userManager.FindByIdAsync(
                member.ApplicationUserId);

            if (user != null)
            {
                // Remove Member role if exists
                if (await _userManager.IsInRoleAsync(user, "Member"))
                {
                    await _userManager.RemoveFromRoleAsync(
                        user,
                        "Member");
                }

                // Ensure User role
                if (!await _userManager.IsInRoleAsync(user, "User"))
                {
                    await _userManager.AddToRoleAsync(
                        user,
                        "User");
                }

                // Create Notification
                _context.Notifications.Add(new Notification
                {
                    MemberId = user.Id,
                    Message = "Your membership request has been rejected by the administrator. Please contact the library for more information.",
                    Link = "/Membership",
                    IsRead = false,
                    CreatedOn = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["Error"] =
                $"Membership rejected for {member.Name}.";

            return RedirectToAction(nameof(Index));
        }
    }
}

using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    public class MembershipController : Controller
    {
        private readonly AppDbContext _context;

        public MembershipController(AppDbContext context)
        {
            _context = context;
        }

        // ================= LIST =================
        public async Task<IActionResult> Index()
        {
            var data = await _context.Memberships
                .Include(m => m.Member)
                .OrderByDescending(m => m.StartDate)
                .ToListAsync();

            return View(data);
        }

        // ================= CREATE GET =================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Members = new SelectList(
                await _context.Members.ToListAsync(),
                "Id",
                "Name"
            );

            return View();
        }

        // ================= CREATE POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Membership membership)
        {
            var alreadyActive = await _context.Memberships
                .AnyAsync(m =>
                    m.MemberId == membership.MemberId &&
                    m.IsActive &&
                    m.EndDate > DateTime.Now);

            if (alreadyActive)
            {
                TempData["Error"] = "Member already has active membership!";
                return RedirectToAction(nameof(Create));
            }

            // START DATE
            membership.StartDate = DateTime.Now;

            // END DATE
            membership.EndDate = DateTime.Now.AddMonths(membership.DurationMonths);

            membership.IsActive = true;

            // ================= FEES =================

            if (membership.MembershipType == "Student")
            {
                if (membership.DurationMonths == 3)
                    membership.Fee = 400;

                else if (membership.DurationMonths == 6)
                    membership.Fee = 600;

                else if (membership.DurationMonths == 12)
                    membership.Fee = 1000;
            }
            else if (membership.MembershipType == "Regular")
            {
                if (membership.DurationMonths == 3)
                    membership.Fee = 600;

                else if (membership.DurationMonths == 6)
                    membership.Fee = 1000;

                else if (membership.DurationMonths == 12)
                    membership.Fee = 1500;
            }
            else if (membership.MembershipType == "Premium")
            {
                if (membership.DurationMonths == 3)
                    membership.Fee = 1000;

                else if (membership.DurationMonths == 6)
                    membership.Fee = 1800;

                else if (membership.DurationMonths == 12)
                    membership.Fee = 3000;
            }

            _context.Memberships.Add(membership);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Membership added successfully!";

            return RedirectToAction(nameof(Index));
        }
    }
}
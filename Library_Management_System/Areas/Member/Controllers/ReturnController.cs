using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize(Roles = "Member,User")]
    public class ReturnController : Controller
    {
        private readonly AppDbContext _context;

        public ReturnController(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // RETURN PAGE
        // =========================
        [HttpGet]
        public async Task<IActionResult> Index(int id)
        {
            var borrow = await _context.BorrowRecords
                .Include(x => x.Book)
                .ThenInclude(x => x.Author)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (borrow == null)
                return NotFound();

            if (borrow.ReturnedOn != null)
            {
                TempData["Error"] = "Book already returned.";
                return RedirectToAction("Index", "BorrowHistory");
            }

            int lateDays = Math.Max(0,
                (DateTime.Now.Date - borrow.DueDate.Date).Days);

            borrow.DaysLate = lateDays;

            ViewBag.LateDays = lateDays;
            ViewBag.LateFine = lateDays * borrow.FinePerDay;

            return View(borrow);
        }

        // =========================
        // RETURN CONFIRM
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id, string returnCondition)
        {
            var borrow = await _context.BorrowRecords
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (borrow == null)
                return NotFound();

            if (borrow.ReturnedOn != null)
            {
                TempData["Error"] = "Book already returned.";
                return RedirectToAction("Index", "BorrowHistory");
            }

            // =========================
            // RETURN INFO
            // =========================

            borrow.ReturnedOn = DateTime.Now;
            borrow.ReturnCondition = returnCondition;

            int lateDays = Math.Max(
                0,
                (borrow.ReturnedOn.Value.Date - borrow.DueDate.Date).Days);

            borrow.DaysLate = lateDays;

            decimal lateFine =
                lateDays * borrow.FinePerDay;

            borrow.FineAmount = lateFine;

            // =========================
            // RESET VALUES
            // =========================

            borrow.DamageCharge = 0;
            borrow.LostBookCharge = 0;
            borrow.ExtraCharge = 0;
            borrow.RefundAmount = 0;

            // =========================
            // CONDITION CHARGES
            // =========================

            if (returnCondition == "Damaged")
            {
                borrow.DamageCharge = 100;
            }
            else if (returnCondition == "Lost")
            {
                borrow.LostBookCharge =
                    borrow.Book?.DepositAmount ?? 0;
            }

            // =========================
            // DEPOSIT VALUE
            // =========================

            decimal depositValue =
                borrow.SecurityDeposit > 0
                ? borrow.SecurityDeposit
                : (borrow.Book?.DepositAmount ?? 0);

            // =========================
            // NON MEMBER LOGIC
            // =========================

            if (borrow.IsNonMemberBorrow)
            {
                decimal totalCharges =
                    borrow.FineAmount +
                    borrow.DamageCharge +
                    borrow.LostBookCharge;

                borrow.RefundAmount =
                    depositValue - totalCharges;

                if (borrow.RefundAmount < 0)
                {
                    borrow.ExtraCharge =
                        Math.Abs(borrow.RefundAmount);

                    borrow.RefundAmount = 0;

                    borrow.FinePaid = false;
                }
                else
                {
                    borrow.ExtraCharge = 0;
                    borrow.FinePaid = true;
                }

                borrow.RefundProcessed = false;
            }
            //else
            //{
            //// =========================
            //// MEMBER LOGIC
            //// =========================

            //decimal totalFine =
            //    borrow.FineAmount +
            //    borrow.DamageCharge +
            //    borrow.LostBookCharge;

            //if (totalFine > 0)
            //{
            //    borrow.FinePaid = false;

            //    await _context.SaveChangesAsync();

            //    TempData["Info"] =
            //        $"Fine payment required: ₹{totalFine}";

            //    return RedirectToAction(
            //        "FinePayment",
            //        "Payment",
            //        new { id = borrow.Id });
            //}

            //borrow.FinePaid = true;


            //}
            else
            {
                decimal totalFine = 0;

                totalFine += borrow.FineAmount;

                if (returnCondition == "Damaged")
                {
                    totalFine += 100;
                }

                if (returnCondition == "Lost")
                {
                    totalFine +=
                        borrow.Book?.DepositAmount ?? 0;
                }

                borrow.FineAmount = totalFine;

                if (totalFine > 0)
                {
                    borrow.FinePaid = false;

                    await _context.SaveChangesAsync();

                    TempData["Info"] =
                        $"Fine payment required: ₹{totalFine}";

                    return RedirectToAction(
                        "FinePayment",
                        "Payment",
                        new { id = borrow.Id });
                }

                borrow.FinePaid = true;

                borrow.RefundAmount = 0;
                borrow.ExtraCharge = 0;
            }

            // =========================
            // FINAL STATUS
            // =========================

            borrow.Status = "Returned";
            borrow.ReturnStatus = "Completed";

            // =========================
            // STOCK UPDATE
            // =========================

            if (borrow.Book != null &&
                returnCondition != "Lost")
            {
                borrow.Book.AvailableCopies++;
            }

            await _context.SaveChangesAsync();

            // =========================
            // SUCCESS MESSAGE
            // =========================

            TempData["SuccessTitle"] =
                "Book Returned Successfully";

            TempData["SuccessDetails"] =
        $@"Security Deposit : ₹{depositValue}
Borrow Fee : ₹{borrow.BorrowFee}
Late Fine : ₹{borrow.FineAmount}
Damage Charge : ₹{borrow.DamageCharge}
Lost Book Charge : ₹{borrow.LostBookCharge}
Refund Amount : ₹{borrow.RefundAmount}
Extra Charge : ₹{borrow.ExtraCharge}
Status : Returned";

            return RedirectToAction(
                "Index",
                "BorrowHistory");
        }
    }
}

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

    public ReturnController(
        AppDbContext context)
        {
            _context = context;
        }

        // RETURN PAGE

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

            // Preview Fine

            int lateDays = Math.Max(
                0,
                (DateTime.Now.Date - borrow.DueDate.Date).Days);

            borrow.DaysLate = lateDays;

            ViewBag.LateDays = lateDays;
            ViewBag.LateFine = lateDays * borrow.FinePerDay;

            return View(borrow);
        }

        // RETURN CONFIRM

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(
    int id,
    string returnCondition)
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

            //----------------------------------
            // RETURN DETAILS
            //----------------------------------

            borrow.ReturnedOn = DateTime.Now;
            borrow.ReturnCondition = returnCondition;

            int lateDays = Math.Max(
                0,
                (borrow.ReturnedOn.Value.Date -
                 borrow.DueDate.Date).Days);

            borrow.DaysLate = lateDays;

            decimal finePerDay = borrow.FinePerDay;

            decimal lateFine =
                lateDays * finePerDay;

            //----------------------------------
            // RESET OLD VALUES
            //----------------------------------

            borrow.FineAmount = 0;
            borrow.DamageCharge = 0;
            borrow.LostBookCharge = 0;
            borrow.ExtraCharge = 0;
            borrow.RefundAmount = 0;

            //----------------------------------
            // MEMBER
            //----------------------------------

            if (!borrow.IsNonMemberBorrow)
            {
                decimal totalCharge = lateFine;

                if (returnCondition == "Damaged")
                {
                    borrow.DamageCharge = 100;
                    totalCharge += 100;
                }
                else if (returnCondition == "Lost")
                {
                    decimal bookPrice =
                        borrow.Book?.DepositAmount ?? 0;

                    borrow.LostBookCharge = bookPrice;
                    totalCharge += bookPrice;
                }

                borrow.FineAmount = totalCharge;

                if (totalCharge > 0)
                {
                    borrow.FinePaid = false;

                    await _context.SaveChangesAsync();

                    return RedirectToAction(
                        "FinePayment",
                        "Payment",
                        new { id = borrow.Id });
                }

                borrow.FinePaid = true;
                borrow.Status = "Returned";

                if (returnCondition != "Lost")
                {
                    borrow.Book.AvailableCopies++;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Book returned successfully.";

                return RedirectToAction(
                    "Index",
                    "BorrowHistory");
            }

            //----------------------------------
            // NON MEMBER
            //----------------------------------

            decimal totalCharges = lateFine;

            if (returnCondition == "Damaged")
            {
                borrow.DamageCharge = 100;
                totalCharges += 100;
            }

            if (returnCondition == "Lost")
            {
                decimal bookPrice =
                    borrow.Book?.DepositAmount ?? 0;

                borrow.LostBookCharge = bookPrice;
                totalCharges += bookPrice;
            }

            borrow.FineAmount = totalCharges;

            decimal deposit =
                borrow.SecurityDeposit;

            //----------------------------------
            // REFUND CALCULATION
            //----------------------------------

            if (totalCharges > deposit)
            {
                borrow.ExtraCharge =
                    totalCharges - deposit;

                borrow.RefundAmount = 0;
            }
            else
            {
                borrow.RefundAmount =
                    deposit - totalCharges;
            }

            borrow.RefundProcessed = false;

            //----------------------------------
            // EXTRA PAYMENT REQUIRED
            //----------------------------------

            if (borrow.ExtraCharge > 0)
            {
                borrow.FinePaid = false;

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    "FinePayment",
                    "Payment",
                    new { id = borrow.Id });
            }

            //----------------------------------
            // SUCCESS RETURN
            //----------------------------------

            borrow.FinePaid = true;
            borrow.Status = "Returned";

            if (returnCondition != "Lost")
            {
                borrow.Book.AvailableCopies++;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Book returned successfully. Refund Amount ₹{borrow.RefundAmount}. Refund will be credited within 3-4 working days.";

            return RedirectToAction(
                "Index",
                "BorrowHistory");
        }

    }

}

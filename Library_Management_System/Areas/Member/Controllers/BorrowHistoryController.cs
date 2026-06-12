using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Library_Management_System.ViewModels;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize(Roles = "Member,User")]
    public class BorrowHistoryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public BorrowHistoryController(AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(string? status)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var memberId = await _context.Members
                .Where(x => x.ApplicationUserId == userId)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            var borrowRecords = await _context.BorrowRecords
                .Include(x => x.Book)
                    .ThenInclude(x => x.Author)
                .Where(x =>
                    x.ApplicationUserId == userId ||
                    (memberId.HasValue && x.MemberId == memberId.Value))
                .OrderByDescending(x => x.IssuedOn)
                .ToListAsync();

            var query = borrowRecords
                .GroupBy(x => x.BookId)
                .Select(g => new
                {
                    Borrow = g.First(),
                    BorrowCount = g.Count()
                })
                .AsQueryable();

            // =========================
            // FILTERS
            // =========================

            if (!string.IsNullOrEmpty(status))
            {
                if (status.ToLower() == "active")
                {
                    query = query.Where(x =>
                        x.Borrow.ReturnedOn == null &&
                        x.Borrow.DueDate >= DateTime.Now);
                }
                else if (status.ToLower() == "returned")
                {
                    query = query.Where(x =>
                        x.Borrow.ReturnedOn != null);
                }
                else if (status.ToLower() == "overdue")
                {
                    query = query.Where(x =>
                        x.Borrow.ReturnedOn == null &&
                        x.Borrow.DueDate < DateTime.Now);
                }
            }

            var history = query
                .OrderByDescending(x => x.Borrow.IssuedOn)
                .Select(x => new BorrowHistoryViewModel
                {
                    Id = x.Borrow.Id,

                    BookTitle =x.Borrow.Book != null
                            ? x.Borrow.Book.Title
                            : "",

                    Author =x.Borrow.Book != null &&
                            x.Borrow.Book.Author != null
                                ? x.Borrow.Book.Author.Name
                                : "",

                    BorrowDate =
                        x.Borrow.IssuedOn,

                    DueDate =
                        x.Borrow.DueDate,

                    ReturnDate =
                        x.Borrow.ReturnedOn,

                    DaysLate =
                        x.Borrow.DaysLate,

                    FinePerDay =
                        x.Borrow.FinePerDay,

                    FineAmount =
                        x.Borrow.FineAmount,

                    FinePaid =
                        x.Borrow.FinePaid,

                    BorrowCount =
                        x.BorrowCount,

                    // Non Member Fields

                    IsNonMemberBorrow =
                        x.Borrow.IsNonMemberBorrow,

                    BorrowFee =
                        x.Borrow.BorrowFee,

                    SecurityDeposit =
                        x.Borrow.SecurityDeposit,

                    RefundAmount =
                        x.Borrow.RefundAmount,

                    DamageCharge =
                        x.Borrow.DamageCharge,

                    LostBookCharge =
                        x.Borrow.LostBookCharge,

                    Status =
                        x.Borrow.ReturnedOn != null
                            ? "Returned"
                            : x.Borrow.DueDate < DateTime.Now
                                ? "Overdue"
                                : "Active"
                })
                .ToList();

            ViewBag.CurrentStatus = status;

            ViewBag.TotalBooks =
                history.Sum(x => x.BorrowCount);

            ViewBag.ActiveBooks =
                history.Count(x => x.Status == "Active");

            ViewBag.OverdueBooks =
                history.Count(x => x.Status == "Overdue");

            ViewBag.ReturnedBooks =
                history.Count(x => x.Status == "Returned");

            return View(history);
        }
        // RETURN PAGE
        [HttpGet]
        public async Task<IActionResult> ReturnBook(int id)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var borrow = await _context.BorrowRecords
                .Include(x => x.Book)
                .ThenInclude(x => x.Author)
                .FirstOrDefaultAsync(x =>
                    x.Id == id);

            if (borrow == null)
                return NotFound();

            return View(borrow);
        }

        // RETURN CONFIRM

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBookConfirmed(
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
                return RedirectToAction(nameof(Index));
            }

            borrow.ReturnedOn = DateTime.Now;
            borrow.ReturnCondition = returnCondition;

            int lateDays =
                Math.Max(
                    0,
                    (borrow.ReturnedOn.Value.Date -
                     borrow.DueDate.Date).Days);

            borrow.DaysLate = lateDays;
            borrow.FinePerDay = 10;

            decimal lateFine =
                lateDays * borrow.FinePerDay;

            borrow.FineAmount = 0;
            borrow.DamageCharge = 0;
            borrow.LostBookCharge = 0;
            borrow.ExtraCharge = 0;

            // ==========================
            // MEMBER
            // ==========================

            if (!borrow.IsNonMemberBorrow)
            {
                if (returnCondition == "Damaged")
                {
                    borrow.DamageCharge = 100;

                    borrow.FineAmount =
                        lateFine +
                        borrow.DamageCharge;
                }
                else if (returnCondition == "Lost")
                {
                    decimal bookPrice =
                        borrow.Book?.DepositAmount ?? 0;

                    borrow.LostBookCharge =
                        bookPrice;

                    borrow.FineAmount =
                        lateFine +
                        bookPrice;
                }

                if (borrow.FineAmount > 0)
                {
                    borrow.FinePaid = false;

                    await _context.SaveChangesAsync();

                    return RedirectToAction(
                        nameof(FinePayment),
                        new { id = borrow.Id });
                }

                borrow.FinePaid = true;
                borrow.Status = "Returned";

                if (borrow.Book != null &&
                    returnCondition != "Lost")
                {
                    borrow.Book.AvailableCopies++;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Book returned successfully.";

                return RedirectToAction(nameof(Index));
            }

            // ==========================
            // NON MEMBER
            // ==========================

            decimal totalCharges = lateFine;

            if (returnCondition == "Damaged")
            {
                borrow.DamageCharge = 100;
                totalCharges += borrow.DamageCharge;
            }

            if (returnCondition == "Lost")
            {
                decimal bookPrice =
                    borrow.Book?.DepositAmount ?? 0;

                borrow.LostBookCharge =
                    bookPrice;

                totalCharges += bookPrice;
            }

            borrow.FineAmount = totalCharges;

            decimal deposit =
                borrow.SecurityDeposit;

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

            if (borrow.FineAmount > 0 ||
                borrow.ExtraCharge > 0)
            {
                borrow.FinePaid = false;

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    nameof(FinePayment),
                    new { id = borrow.Id });
            }

            borrow.FinePaid = true;
            borrow.Status = "Returned";

            if (borrow.Book != null &&
                returnCondition != "Lost")
            {
                borrow.Book.AvailableCopies++;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Refund Amount ₹{borrow.RefundAmount}";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> FinePayment(int id)
        {
            var borrow = await _context.BorrowRecords
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (borrow == null)
                return NotFound();

            string key = _configuration["Razorpay:Key"];
            string secret = _configuration["Razorpay:Secret"];

            Razorpay.Api.RazorpayClient client =
                new Razorpay.Api.RazorpayClient(key, secret);

            Dictionary<string, object> options = new();

            options.Add("amount",
                Convert.ToInt32(borrow.FineAmount * 100));

            options.Add("currency", "INR");

            options.Add("receipt",
                $"FINE_{borrow.Id}");

            Razorpay.Api.Order order =
                client.Order.Create(options);

            ViewBag.RazorpayKey = key;
            ViewBag.OrderId = order["id"].ToString();

            return View(borrow);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinePaymentSuccess(
    int borrowId,
    string razorpayPaymentId,
    string razorpayOrderId,
    string razorpaySignature)
        {
            string generatedSignature;

            using (var hmac = new HMACSHA256(
                Encoding.UTF8.GetBytes(
                    _configuration["Razorpay:Secret"])))
            {
                var hash = hmac.ComputeHash(
                    Encoding.UTF8.GetBytes(
                        razorpayOrderId + "|" + razorpayPaymentId));

                generatedSignature =
                    BitConverter.ToString(hash)
                    .Replace("-", "")
                    .ToLower();
            }

            if (generatedSignature != razorpaySignature)
            {
                TempData["Error"] =
                    "Payment verification failed.";

                return RedirectToAction(nameof(Index));
            }

            var borrow = await _context.BorrowRecords
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == borrowId);

            if (borrow == null)
                return NotFound();

            borrow.FinePaid = true;
            borrow.Status = "Returned";

            if (borrow.Book != null &&
                borrow.ReturnCondition != "Lost")
            {
                borrow.Book.AvailableCopies++;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Fine payment completed successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}

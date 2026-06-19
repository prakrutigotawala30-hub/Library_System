using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Razorpay.Api;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RefundController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public RefundController(
            AppDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // =========================
        // REFUND LIST
        // =========================

        public async Task<IActionResult> Index()
        {
            var refunds = await _context.BorrowRecords
                .Include(x => x.Book)
                .Include(x => x.Member)
                .Where(x =>
                    x.Status == "Returned" &&
                    x.IsNonMemberBorrow &&
                    x.RefundAmount > 0)
                .OrderByDescending(x => x.ReturnedOn)
                .ToListAsync();

            return View(refunds);
        }

        // =========================
        // REFUND DETAILS
        // =========================

        public async Task<IActionResult> Details(int id)
        {
            var record = await _context.BorrowRecords
                .Include(x => x.Book)
                .Include(x => x.Member)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (record == null)
                return NotFound();

            return View(record);
        }

        // =========================
        // PROCESS REFUND
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRefund(int id)
        {
            var record = await _context.BorrowRecords
                .Include(x => x.Book)
                .Include(x => x.Member)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (record == null)
                return NotFound();

            if (record.RefundProcessed)
            {
                TempData["Error"] =
                    "Refund already processed.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (record.RefundAmount <= 0)
            {
                TempData["Error"] =
                    "No refund amount available.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (string.IsNullOrWhiteSpace(
                record.RazorpayPaymentId))
            {
                TempData["Error"] =
                    "Original Razorpay payment id not found.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            try
            {
                string key =
                    _configuration["Razorpay:Key"];

                string secret =
                    _configuration["Razorpay:Secret"];

                RazorpayClient client =
                    new RazorpayClient(
                        key,
                        secret);

                Payment payment =
                    client.Payment.Fetch(
                        record.RazorpayPaymentId);

                Dictionary<string, object> options =
                    new Dictionary<string, object>();

                options.Add(
                    "amount",
                    (int)(record.RefundAmount * 100));

                options.Add(
                    "speed",
                    "normal");

                Refund refund =
                    payment.Refund(options);

                record.RefundProcessed = true;

                record.RefundDate =
                    DateTime.Now;

                record.RazorpayRefundId =
                    refund["id"]?.ToString();

                // =========================
                // USER NOTIFICATION
                // =========================

                if (!string.IsNullOrEmpty(record.ApplicationUserId))
                {
                    _context.Notifications.Add(new Notification
                    {
                        MemberId = record.ApplicationUserId,

                        Message =
                            $"Your refund of ₹{record.RefundAmount} has been processed successfully for '{record.Book?.Title}'. Refund ID: {record.RazorpayRefundId}",

                        Link = "/Member/BorrowHistory",

                        IsRead = false,

                        CreatedOn = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    $"Refund of ₹{record.RefundAmount} processed successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    ex.Message;
            }

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
    }
}

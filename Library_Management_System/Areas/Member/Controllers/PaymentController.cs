using System.Security.Cryptography;
using System.Text;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize(Roles = "Member,User")]
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

    public PaymentController(
        AppDbContext context,
        IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> FinePayment(int id)
        {
            var borrow = await _context.BorrowRecords
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (borrow == null)
                return NotFound();

            decimal payableAmount =
                borrow.IsNonMemberBorrow
                    ? borrow.ExtraCharge
                    : borrow.FineAmount;

            if (payableAmount <= 0)
            {
                TempData["Error"] = "No payment required.";
                return RedirectToAction("Index", "BorrowHistory");
            }

            string key = _configuration["Razorpay:Key"];

            var client = new Razorpay.Api.RazorpayClient(
                key,
                _configuration["Razorpay:Secret"]);

            Dictionary<string, object> options = new();

            options.Add(
                "amount",
                Convert.ToInt32(payableAmount * 100));

            options.Add("currency", "INR");

            options.Add(
                "receipt",
                $"FINE_{borrow.Id}");

            var order = client.Order.Create(options);

            ViewBag.RazorpayKey = key;
            ViewBag.OrderId = order["id"].ToString();

            return View("FinePayment", borrow);
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

            using (var hmac =
                new HMACSHA256(
                    Encoding.UTF8.GetBytes(
                        _configuration["Razorpay:Secret"])))
            {
                var hash =
                    hmac.ComputeHash(
                        Encoding.UTF8.GetBytes(
                            razorpayOrderId + "|" +
                            razorpayPaymentId));

                generatedSignature =
                    BitConverter.ToString(hash)
                    .Replace("-", "")
                    .ToLower();
            }

            if (generatedSignature != razorpaySignature)
            {
                TempData["Error"] =
                    "Payment verification failed.";

                return RedirectToAction(
                    "Index",
                    "BorrowHistory");
            }

            var borrow = await _context.BorrowRecords
                .Include(x => x.Book)
                .FirstOrDefaultAsync(
                    x => x.Id == borrowId);

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
    "Payment completed successfully and book return has been finalized.";

            return RedirectToAction(
                "Index",
                "BorrowHistory");
        }
    }
}

using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReservationsController : Controller
    {
        private readonly AppDbContext _context;

        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? status)
        {
            var reservations = _context.Reservations
                .Include(r => r.Book)
                    .ThenInclude(b => b.Author)
                .Include(r => r.Member)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ReservationStatus>(
                    status,
                    true,
                    out var reservationStatus))
            {
                reservations =
                    reservations.Where(r =>
                        r.Status == reservationStatus);
            }

            ViewBag.CurrentStatus = status;

            ViewBag.WaitingCount =
                await _context.Reservations
                .CountAsync(r =>
                    r.Status == ReservationStatus.Waiting);

            ViewBag.CompletedCount =
                await _context.Reservations
                .CountAsync(r =>
                    r.Status == ReservationStatus.Completed);

            ViewBag.CancelledCount =
                await _context.Reservations
                .CountAsync(r =>
                    r.Status == ReservationStatus.Cancelled);

            var result = await reservations
                .OrderByDescending(r => r.ReservedOn)
                .ToListAsync();

            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fulfill(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Book)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
                return NotFound();

            if (reservation.Status != ReservationStatus.Waiting)
            {
                TempData["Error"] =
                    "Only waiting reservations can be fulfilled.";

                return RedirectToAction(nameof(Index));
            }

            if (reservation.Book == null)
            {
                TempData["Error"] =
                    "Book not found.";

                return RedirectToAction(nameof(Index));
            }

            int quantity = reservation.Quantity > 0
                ? reservation.Quantity
                : 1;

            if (reservation.Book.AvailableCopies < quantity)
            {
                TempData["Error"] =
                    $"Only {reservation.Book.AvailableCopies} copies available.";

                return RedirectToAction(nameof(Index));
            }

            // ==========================
            // FIND MEMBER
            // ==========================

            var member = await _context.Members
                .FirstOrDefaultAsync(m =>
                    m.ApplicationUserId == reservation.MemberId);

            // NON-MEMBER -> CREATE MEMBER RECORD
            if (member == null)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.Id == reservation.MemberId);

                if (user == null)
                {
                    TempData["Error"] =
                        "User account not found.";

                    return RedirectToAction(nameof(Index));
                }

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

            var settings = await _context.LibrarySettings
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new LibrarySettings
                {
                    DefaultLoanDays = 14,
                    FinePerDay = 10
                };
            }

            for (int i = 0; i < quantity; i++)
            {
                _context.BorrowRecords.Add(new BorrowRecord
                {
                    BookId = reservation.BookId,

                    MemberId = member.Id,

                    ApplicationUserId =
                        reservation.MemberId,

                    IssuedOn = DateTime.Now,

                    DueDate =
                        DateTime.Now.AddDays(
                            5),

                    FinePerDay =
                        settings.FinePerDay,

                    FineAmount = 0,

                    DaysLate = 0,

                    RenewCount = 0,

                    Status = "Issued",

                    ReturnStatus = "Pending",

                    FinePaid = false,

                    // =========================
                    // COPY PAYMENT DATA
                    // =========================

                    BorrowFee =
                        reservation.BorrowFee,

                    SecurityDeposit =
                        reservation.SecurityDeposit,

                    RazorpayPaymentId =
                        reservation.RazorpayPaymentId,

                    RazorpayOrderId =
                        reservation.RazorpayOrderId,

                    IsNonMemberBorrow =
                        reservation.PaymentRequired,

                    // =========================
                    // RETURN / REFUND
                    // =========================

                    RefundAmount = 0,

                    RefundProcessed = false,

                    DamageCharge = 0,

                    LostBookCharge = 0,

                    ExtraCharge = 0
                });
            }

            reservation.Book.AvailableCopies -= quantity;

            reservation.Status = ReservationStatus.Completed;

            if (!string.IsNullOrEmpty(reservation.MemberId))
            {
                _context.Notifications.Add(new Notification
                {
                    MemberId = reservation.MemberId,
                    Message =
                        $"Your reservation for '{reservation.Book.Title}' has been approved and issued.",
                    Link = "/Member/BorrowHistory"
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Reservation fulfilled successfully. {quantity} book(s) issued.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Book)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
                return NotFound();

            if (reservation.Status == ReservationStatus.Completed)
            {
                TempData["Error"] =
                    "Completed reservations cannot be cancelled.";

                return RedirectToAction(nameof(Index));
            }

            reservation.Status = ReservationStatus.Cancelled;

            if (!string.IsNullOrEmpty(reservation.MemberId))
            {
                string message = reservation.PaymentRequired
                    ? $"Your paid reservation for '{reservation.Book?.Title}' has been cancelled. Please contact librarian for refund details."
                    : $"Your reservation for '{reservation.Book?.Title}' has been cancelled.";

                _context.Notifications.Add(new Notification
                {
                    MemberId = reservation.MemberId,
                    Message = message,
                    Link = "/Member/MyReservations"
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Reservation cancelled successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}

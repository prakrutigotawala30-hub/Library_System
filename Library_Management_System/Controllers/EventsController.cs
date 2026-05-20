using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Controllers
{
    public class EventsController : Controller
    {
        private readonly AppDbContext _context;

        public EventsController(AppDbContext context)
        {
            _context = context;
        }

        // UPCOMING EVENTS
        public async Task<IActionResult> Index()
        {
            var upcomingEvents = await _context.Events
                .Where(e => e.Date >= DateTime.Today)
                .OrderBy(e => e.Date)
                .ToListAsync();

            return View(upcomingEvents);
        }

        // EVENT DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null)
                return NotFound();

            return View(ev);
        }

        // PAST EVENTS
        public async Task<IActionResult> Past()
        {
            var pastEvents = await _context.Events
                .Where(e => e.Date < DateTime.Today)
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            return View(pastEvents);
        }
    }
}
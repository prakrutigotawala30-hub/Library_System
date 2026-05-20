using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EventsController : Controller
    {
        private readonly AppDbContext _context;

        public EventsController(AppDbContext context)
        {
            _context = context;
        }

        // INDEX
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            return View(events);
        }

        // DETAILS
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

        // CREATE GET
        public IActionResult Create()
        {
            return View();
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event ev)
        {
            if (ModelState.IsValid)
            {
                _context.Events.Add(ev);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Event created successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(ev);
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int id)
        {
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
                return NotFound();

            return View(ev);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event ev)
        {
            if (id != ev.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(ev);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Event updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(ev);
        }

        // DELETE GET
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null)
                return NotFound();

            return View(ev);
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ev = await _context.Events.FindAsync(id);

            if (ev != null)
            {
                _context.Events.Remove(ev);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Event deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
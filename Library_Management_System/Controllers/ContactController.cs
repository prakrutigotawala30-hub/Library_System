using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Library_Management_System.Services;
using Library_Management_System.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Library_Management_System.Controllers
{
    public class ContactController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public ContactController(
            AppDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(ContactFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var message = new ContactMessage
            {
                Name = model.Name,
                Email = model.Email,
                Subject = model.Subject,
                Message = model.Message
            };

            _context.ContactMessages.Add(message);

            await _context.SaveChangesAsync();

            // ADMIN NOTIFICATION EMAIL

            string adminBody = $@"
                <h2>New Contact Message</h2>

                <p><strong>Name:</strong> {model.Name}</p>

                <p><strong>Email:</strong> {model.Email}</p>

                <p><strong>Subject:</strong> {model.Subject}</p>

                <p><strong>Message:</strong></p>

                <p>{model.Message}</p>
            ";

            await _emailService.SendEmailAsync(
                "prakrutigotawala30@gmail.com",
                "New Contact Form Message",
                adminBody);

            return RedirectToAction("ThankYou");
        }

        public IActionResult ThankYou()
        {
            return View();
        }
    }
}
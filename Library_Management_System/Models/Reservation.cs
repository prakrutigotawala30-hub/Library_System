using Library_Management_System.Models;
using LibraryManagementSystem.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        // 👤 MemberId from IdentityUser (ApplicationUser)
        [Required(ErrorMessage = "Member is required")]
        [StringLength(450)]
        public string MemberId { get; set; }

        [ForeignKey("MemberId")]
        public ApplicationUser Member { get; set; }

        // 📚 Book reference
        [Required(ErrorMessage = "Book is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid Book selected")]
        public int BookId { get; set; }

        public DateTime ReservedOn { get; set; } = DateTime.Now;

        // 📌 Reservation status
        [Required]
        public ReservationStatus Status { get; set; } = ReservationStatus.Waiting;
    }
}